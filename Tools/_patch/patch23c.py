# 23차 patch C: 원격 브릿지용 런타임 훅 — StageManager.DebugClear, MobileSwipeInput.Inject, 디버그 키 래퍼
import io, sys, re
ROOT = 'Assets/_CoastRun/Scripts/'
def rw(path, pairs):
    p = ROOT + path
    s = io.open(p, encoding='utf-8').read()
    for old, new in pairs:
        if old not in s:
            print('MISSING in', path, ':', old[:80]); sys.exit(1)
        s = s.replace(old, new, 1)
    io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
    print('ok', path)

rw('Core/StageManager.cs', [
('''        /// Editor aid: warp the player to 30 m before the finish so a clear can be tested.''',
'''        /// 23차: 원격(MCP) 디버그 — 즉시 클리어.
        public void DebugClear() { if (_stageActive && !ArcadeRun.Active) ClearCurrent(); }

        /// Editor aid: warp the player to 30 m before the finish so a clear can be tested.'''),
])
rw('Input/MobileSwipeInput.cs', [
('''        public int ConsumeLaneDelta()''',
'''        /// 23차: 원격(MCP) 입력 주입 — 실제 스와이프와 같은 버퍼를 탄다.
        public void Inject(int laneDir, bool jump, bool crouch)
        {
            float now = Time.unscaledTime;
            if (laneDir != 0) { _laneDir = laneDir; _laneStamp = now; }
            if (jump) _jumpStamp = now;
            if (crouch) _crouchStamp = now;
        }

        public int ConsumeLaneDelta()'''),
])
# 디버그 키 → CoastRemoteKeys.Down (게임 조작키 A/D/W/S/화살표/스페이스는 그대로)
files = ['Core/StageManager.cs', 'Economy/ObstacleSpawner.cs', 'Visual/MonochromeWorld.cs', 'Raising/RaisingUI.cs', 'UI/ArcadeUI.cs',
         'Progression/UpgradeShopHotkeys.cs', 'Meta/CoastPrefs.cs', 'UI/CoastUiCanvas.cs']
keep = {'A', 'D', 'W', 'S', 'LeftArrow', 'RightArrow', 'UpArrow', 'DownArrow', 'Space', 'Escape'}
for f in files:
    p = ROOT + f
    s = io.open(p, encoding='utf-8').read()
    def sub(m):
        k = m.group(1)
        return m.group(0) if k in keep else 'CoastRemoteKeys.Down(KeyCode.' + k + ')'
    s2 = re.sub(r'Input\.GetKeyDown\(KeyCode\.([A-Za-z0-9]+)\)', sub, s)
    if s2 != s:
        io.open(p, 'w', encoding='utf-8', newline='\n').write(s2); print('keys', f)
print('ALL OK')
