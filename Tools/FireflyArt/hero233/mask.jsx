// 233차: raw → 피사체 선택(Select Subject) 마스크 → 투명 PNG(cut/)
function s2t(s){return stringIDToTypeID(s);}
function maskOne(name){
  var doc = app.open(new File("C:/dev/game/Tools/FireflyArt/hero233/raw/" + name + ".png"));
  doc.activeLayer.isBackgroundLayer = false;
  app.runMenuItem(s2t("autoCutout"));
  var m = new ActionDescriptor(); m.putClass(s2t("new"), s2t("channel"));
  var r = new ActionReference(); r.putEnumerated(s2t("channel"), s2t("channel"), s2t("mask")); m.putReference(s2t("at"), r);
  m.putEnumerated(s2t("using"), s2t("userMaskEnabled"), s2t("revealSelection"));
  executeAction(s2t("make"), m, DialogModes.NO);
  doc.saveAs(new File("C:/dev/game/Tools/FireflyArt/hero233/cut/" + name + ".png"), new PNGSaveOptions(), true);
  doc.close(SaveOptions.DONOTSAVECHANGES);
  return "ok " + name;
}
function maskAll(){
  var fs = new Folder("C:/dev/game/Tools/FireflyArt/hero233/raw").getFiles("*.png"); var out=[];
  for (var i=0;i<fs.length;i++){ var n=decodeURI(fs[i].name).replace(/\.png$/,""); if (new File("C:/dev/game/Tools/FireflyArt/hero233/cut/"+n+".png").exists) continue;
    try{ out.push(maskOne(n)); }catch(e){ out.push("ERR "+n+": "+e); while(app.documents.length) app.activeDocument.close(SaveOptions.DONOTSAVECHANGES);} }
  return out.join("\n");
}
