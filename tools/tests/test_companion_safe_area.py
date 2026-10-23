"""#1119: safe margins are relative to the canvas after the page reserves space beside/below it."""
import json
import shutil
import subprocess
from pathlib import Path

import pytest

SOURCE = Path(__file__).resolve().parents[2] / "unity/Assets/Scripts/UI/SafeArea.jslib"


@pytest.mark.skipif(shutil.which("node") is None, reason="needs node for the shipped .jslib")
@pytest.mark.parametrize("rect,insets,expected", [
    ([0, 0, 390, 844], [47, 0, 34, 0], [47, 0, 34, 0]),
    ([0, 47, 390, 557], [47, 0, 34, 0], [0, 0, 0, 0]),
    ([20, 10, 370, 830], [47, 30, 34, 44], [37, 10, 20, 24]),
])
def test_the_safe_area_only_pads_hardware_that_intersects_the_canvas(rect, insets, expected):
    driver = """
const fs = require('fs'), vm = require('vm');
const [left,top,right,bottom] = JSON.parse(process.argv[1]), raw=JSON.parse(process.argv[2]);
const library={}, heap=new Float32Array(4);
const context={LibraryManager:{library}, mergeInto:Object.assign, HEAPF32:heap,
  Module:{canvas:{getBoundingClientRect:()=>({left,top,right,bottom})}},
  window:{innerWidth:390,innerHeight:844}, document:{createElement:()=>({style:{}}),body:{appendChild(){},removeChild(){}}},
  getComputedStyle:()=>({paddingTop:String(raw[0]),paddingRight:String(raw[1]),paddingBottom:String(raw[2]),paddingLeft:String(raw[3])})};
vm.runInNewContext(fs.readFileSync(process.argv[3],'utf8'),context);
library.StatecraftSafeAreaInsets(0);
console.log(JSON.stringify([...heap]));
"""
    result = subprocess.run(["node", "-e", driver, json.dumps(rect), json.dumps(insets), str(SOURCE)],
                            text=True, capture_output=True, check=True)
    assert json.loads(result.stdout) == expected
