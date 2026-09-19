// Frequency Bridge — Claude MCP(Tools/Mcp/figma_mcp_server.py) 가 보낸 JS 를 플러그인 샌드박스에서 실행한다.
// ui.html 이 127.0.0.1:47010 을 롱폴링해서 명령을 받아 여기로 넘기고, 결과를 다시 서버로 보낸다.
figma.showUI(__html__, { width: 320, height: 200, title: "Frequency Bridge" });

async function loadFont(family, style) {
  try { await figma.loadFontAsync({ family, style }); return { family, style }; }
  catch (e) { await figma.loadFontAsync({ family: "Inter", style: "Regular" }); return { family: "Inter", style: "Regular" }; }
}

// ── 헬퍼(H): MCP 쪽 JS 에서 H.xxx 로 사용 ──────────────────────────────
const H = {
  async fetchBytes(url) {
    const r = await fetch(url);
    if (!r.ok) throw new Error("fetch " + r.status + " " + url);
    return new Uint8Array(await r.arrayBuffer());
  },
  // 이미지 파일 → 원본 크기(최대 maxW 로 축소)의 IMAGE fill 사각형
  async placeImage(url, name, parent, x, y, maxW) {
    const bytes = await H.fetchBytes(url);
    const img = figma.createImage(bytes);
    const size = await img.getSizeAsync();
    let w = size.width, h = size.height;
    if (maxW && w > maxW) { h = h * maxW / w; w = maxW; }
    const rect = figma.createRectangle();
    rect.name = name; rect.resize(Math.max(1, w), Math.max(1, h)); rect.x = x; rect.y = y;
    rect.fills = [{ type: "IMAGE", scaleMode: "FIT", imageHash: img.hash }];
    (parent || figma.currentPage).appendChild(rect);
    return rect;
  },
  // 그리드 셀: 셀 프레임(체커 배경) + 이미지(비율 유지 FIT) + 아래 이름 라벨
  async cellImage(url, name, parent, x, y, cell) {
    const bytes = await H.fetchBytes(url);
    const img = figma.createImage(bytes);
    const size = await img.getSizeAsync();
    const f = figma.createFrame();
    f.name = name; f.resize(cell, cell + 30); f.x = x; f.y = y;
    f.fills = [{ type: "SOLID", color: { r: 0.96, g: 0.96, b: 0.97 } }];
    f.cornerRadius = 8; f.clipsContent = true;
    const k = Math.min((cell - 16) / size.width, (cell - 16) / size.height, 1e9);
    const w = Math.max(1, size.width * k), h = Math.max(1, size.height * k);
    const rect = figma.createRectangle();
    rect.name = "img"; rect.resize(w, h); rect.x = (cell - w) / 2; rect.y = 8 + (cell - 16 - h) / 2;
    rect.fills = [{ type: "IMAGE", scaleMode: "FIT", imageHash: img.hash }];
    f.appendChild(rect);
    const font = await loadFont("Inter", "Regular");
    const t = figma.createText(); t.fontName = font; t.characters = name; t.fontSize = 11;
    t.fills = [{ type: "SOLID", color: { r: 0.25, g: 0.25, b: 0.3 } }];
    t.textAutoResize = "HEIGHT"; t.resize(cell - 16, 20); t.x = 8; t.y = cell + 4; t.textAlignHorizontal = "CENTER";
    t.textTruncation = "ENDING"; t.maxLines = 1;
    f.appendChild(t);
    (parent || figma.currentPage).appendChild(f);
    return f;
  },
  async text(parent, str, x, y, size, bold, color) {
    const font = await loadFont("Inter", bold ? "Bold" : "Regular");
    const t = figma.createText(); t.fontName = font; t.characters = str; t.fontSize = size || 14;
    if (color) t.fills = [{ type: "SOLID", color }];
    t.x = x; t.y = y; (parent || figma.currentPage).appendChild(t); return t;
  },
  async title(page, title, sub) {
    await H.text(page, title, 40, 40, 40, true, { r: 0.1, g: 0.1, b: 0.15 });
    if (sub) await H.text(page, sub, 40, 96, 16, false, { r: 0.45, g: 0.45, b: 0.5 });
  },
  frame(parent, name, x, y, w, h, color) {
    const f = figma.createFrame(); f.name = name; f.x = x; f.y = y; f.resize(w, h);
    if (color) f.fills = [{ type: "SOLID", color }];
    (parent || figma.currentPage).appendChild(f); return f;
  },
  info() { return { doc: figma.root.name, page: figma.currentPage.name, pages: figma.root.children.length }; }
};

function safe(v) {
  try { return JSON.parse(JSON.stringify(v === undefined ? null : v)); } catch (e) { return String(v); }
}

figma.ui.onmessage = async (msg) => {
  if (!msg) return;
  if (msg.type === "info") { figma.ui.postMessage({ type: "info", info: H.info() }); return; }
  if (msg.type !== "cmd") return;
  const { id, code, args } = msg;
  let ok = true, result;
  try {
    const fn = new Function("figma", "args", "H", '"use strict"; return (async () => {\n' + code + "\n})();");
    result = await fn(figma, args || {}, H);
  } catch (e) {
    ok = false; result = String((e && e.stack) || e);
  }
  figma.ui.postMessage({ type: "result", id, ok, result: safe(result) });
};
