// 233차: Firefly 텍스트→이미지 + 피사체 선택 마스크 + PNG 저장
function s2t(s){return stringIDToTypeID(s);}
function genOne(name, w, h, prompt, seed){
  var doc = app.documents.add(UnitValue(w,"px"), UnitValue(h,"px"), 72, name, NewDocumentMode.RGB, DocumentFill.WHITE);
  var d = new ActionDescriptor(); var r = new ActionReference();
  r.putEnumerated(s2t("document"), s2t("ordinal"), s2t("targetEnum")); d.putReference(s2t("null"), r);
  d.putString(s2t("prompt"), prompt);
  var s = new ActionDescriptor();
  s.putString(s2t("gi_PROMPT"), prompt); s.putString(s2t("gi_MODE"), "tti");
  s.putInteger(s2t("gi_SEED"), seed===undefined?-1:seed); s.putInteger(s2t("gi_NUM_STEPS"), -1);
  s.putInteger(s2t("gi_GUIDANCE"), 6); s.putInteger(s2t("gi_SIMILARITY"), 0);
  s.putBoolean(s2t("gi_CROP"), false); s.putBoolean(s2t("gi_DILATE"), false);
  s.putInteger(s2t("gi_CONTENT_PRESERVE"), 0); s.putBoolean(s2t("gi_ENABLE_PROMPT_FILTER"), true);
  s.putBoolean(s2t("dualCrop"), true); s.putString(s2t("gi_ADVANCED"), "{\"enable_mts\":true}");
  d.putObject(s2t("serviceOptionsList"), s2t("syntheticTextToImage"), s);
  d.putString(s2t("serviceID"), "gen_image"); d.putString(s2t("workflow"), "text_to_image");
  executeAction(s2t("syntheticTextToImage"), d, DialogModes.NO);
  // 평면화해서 일반 레이어로 → 배경 흰색 그대로 저장(투명 처리는 PIL에서)
  doc.flatten();
  doc.saveAs(new File("C:/dev/game/Tools/FireflyArt/hero233/raw/" + name + ".png"), new PNGSaveOptions(), true);
  doc.close(SaveOptions.DONOTSAVECHANGES);
  return "ok " + name;
}
