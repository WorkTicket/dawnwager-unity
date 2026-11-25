import sys
from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.launch();pg=b.new_page(viewport={'width':1920,'height':1080})
    pg.on('console',lambda m:print('console',m.text));pg.on('pageerror',lambda e:print('err',e))
    pg.goto('file:///home/claude/proto/index.html');pg.wait_for_timeout(1500)
    pg.screenshot(path=sys.argv[1])
    if len(sys.argv)>2:
        pg.hover('.cardwrap:nth-of-type(3)',force=True) if False else None
    b.close()
