"""Markdown link check: a dead relative link is found; URLs, anchors, code fences and good links are not; the repo is clean."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('md_links', ROOT / 'scripts/oss/md_links.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)


def test_finds_only_dead_relative_links(make_repo, capsys):
    root = make_repo({
        'README.md': '[ok](docs/a.md) [anchor](docs/a.md#x) [url](https://x.example/y) [mail](mailto:someone) [top](#t)\n'
                     '```\n[in a fence](nope.md)\n```\n[ref][one] [odd](a[b])\n[dead](missing.md#s)\n',
        'docs/a.md': '[up](../README.md) [bad](../../README.md)\n',
    })
    assert m.dead_links(root) == [('README.md', 'missing.md#s'), ('docs/a.md', '../../README.md')]
    assert m.main(['--root', str(root)]) == 1
    assert 'docs/a.md: ../../README.md' in capsys.readouterr().out


def test_clean_repo_returns_zero(make_repo):
    root = make_repo({'README.md': '[a](a.md)\n', 'a.md': 'x\n'})
    assert m.main(['--root', str(root)]) == 0


def test_this_repo_has_no_dead_relative_links():
    assert m.dead_links(ROOT) == []


def test_title_badge_escape_and_case(make_repo):
    root = make_repo({
        'README.md': '[t](docs/a.md "title") [t2](docs/a.md \'x y\') [angle](<docs/a.md>) [esc](docs/b%20c.md) [dir](docs/)\n'
                     '[![badge](img.svg)](LICENSING.md) [![b2](img.svg)](GONE.md) [case](Docs/a.md) [root](.) [root2](./)\n',
        'docs/a.md': '[up](..) [up2](../)\n', 'docs/b c.md': 'x\n', 'img.svg': 'x\n', 'LICENSING.md': 'x\n',
    })
    assert m.dead_links(root) == [('README.md', 'GONE.md'), ('README.md', 'Docs/a.md')]


def test_links_in_inline_code_and_tilde_or_long_fences_are_not_links(make_repo):
    root = make_repo({
        'README.md': 'Write links as `` `[text](path/to/file.md)` `` and `[x](gone1.md)`.\n\n'
                     '~~~\n[t](gone2.md)\n~~~\n\n'
                     '````\n```\n[f](gone3.md)\n```\n````\n',
    })
    assert m.dead_links(root) == []


def test_a_real_dead_link_next_to_an_inline_code_span_is_still_found(make_repo):
    root = make_repo({'README.md': 'Use `[a](shown.md)` then [real](missing.md) and `code` [ok](README.md).\n'})
    assert m.dead_links(root) == [('README.md', 'missing.md')]


def test_stray_backtick_and_shorter_fence_run_do_not_hide_real_links(make_repo):
    root = make_repo({
        'README.md': 'a ` b\n\n[one](gone1.md)\n\n````\n```\n[in](x.md)\n````\n[two](gone2.md)\n',
    })
    assert m.dead_links(root) == [('README.md', 'gone1.md'), ('README.md', 'gone2.md')]


def test_an_inline_span_does_not_cross_a_blank_line(make_repo):
    root = make_repo({'README.md': 'a ` b\n\n[one](gone1.md)\n\nc ` d\n'})
    assert m.dead_links(root) == [('README.md', 'gone1.md')]


def test_balanced_parentheses_keep_the_whole_target(make_repo, capsys):
    root = make_repo({
        'README.md': '[guide](docs/setup_(mac).md) [nested](docs/setup_(mac_(arm64)).md)\n'
                     '[title](docs/setup_(mac).md "Mac setup") [angle](<docs/setup_(mac_(arm64)).md>)\n'
                     '[gone](docs/missing_(x).md) [nested gone](docs/missing_(x_(y)).md)\n',
        'docs/setup_(mac).md': 'x\n',
        'docs/setup_(mac_(arm64)).md': 'x\n',
    })
    assert m.dead_links(root) == [('README.md', 'docs/missing_(x).md'),
                                  ('README.md', 'docs/missing_(x_(y)).md')]
    assert m.main(['--root', str(root)]) == 1
    assert capsys.readouterr().out == ('README.md: docs/missing_(x).md\n'
                                     'README.md: docs/missing_(x_(y)).md\n')


def test_parenthesized_targets_do_not_hide_following_links(make_repo):
    root = make_repo({
        'README.md': '[ok](docs/setup_(mac_(arm64)).md) [gone](missing.md) '
                     '[another](docs/other_(x).md) [last](last.md)\n',
        'docs/setup_(mac_(arm64)).md': 'x\n',
    })
    assert m.dead_links(root) == [('README.md', 'missing.md'), ('README.md', 'docs/other_(x).md'),
                                  ('README.md', 'last.md')]
