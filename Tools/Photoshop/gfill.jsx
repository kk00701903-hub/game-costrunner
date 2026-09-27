function gfill(prompt){
var mode = prompt == "" ? "ginp" : "tinp";
var d = new ActionDescriptor();
var r = new ActionReference(); r.putEnumerated(charIDToTypeID("Dcmn"), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
d.putReference(charIDToTypeID("null"), r);
d.putString(stringIDToTypeID("prompt"), prompt);
d.putString(stringIDToTypeID("serviceID"), "clio");
var so = new ActionDescriptor(); var c = new ActionDescriptor();
c.putString(stringIDToTypeID("gi_PROMPT"), prompt);
c.putString(stringIDToTypeID("gi_MODE"), mode);
c.putInteger(stringIDToTypeID("gi_SEED"), -1);
c.putInteger(stringIDToTypeID("gi_NUM_STEPS"), -1);
c.putInteger(stringIDToTypeID("gi_GUIDANCE"), 6);
c.putInteger(stringIDToTypeID("gi_SIMILARITY"), 0);
c.putBoolean(stringIDToTypeID("gi_CROP"), false);
c.putBoolean(stringIDToTypeID("gi_DILATE"), false);
c.putInteger(stringIDToTypeID("gi_CONTENT_PRESERVE"), 0);
c.putBoolean(stringIDToTypeID("gi_ENABLE_PROMPT_FILTER"), true);
c.putBoolean(stringIDToTypeID("dualCrop"), true);
c.putString(stringIDToTypeID("gi_ADVANCED"), '{"enable_mts":true}');
so.putObject(stringIDToTypeID("clio"), stringIDToTypeID("clio"), c);
d.putObject(stringIDToTypeID("serviceOptionsList"), stringIDToTypeID("target"), so);
executeAction(stringIDToTypeID("syntheticFill"), d, DialogModes.NO);
}
function sel(doc, l, t, r, b, ellipse){
  if (!ellipse) { doc.selection.select([[l,t],[r,t],[r,b],[l,b]]); return; }
  var pts=[]; var cx=(l+r)/2, cy=(t+b)/2, rx=(r-l)/2, ry=(b-t)/2;
  for (var k=0;k<48;k++){ var a=k/48*Math.PI*2; pts.push([cx+Math.cos(a)*rx, cy+Math.sin(a)*ry]); }
  doc.selection.select(pts);
}
// steps: [{src:, out:, ops:[[l,t,r,b,prompt,ellipse],...]}]
function run(job){
  var doc = app.open(new File(job.src));
  for (var i=0;i<job.ops.length;i++){ var o=job.ops[i]; sel(doc,o[0],o[1],o[2],o[3],o[5]); gfill(o[4]); doc.flatten(); }
  doc.selection.deselect();
  doc.saveAs(new File(job.out), new PNGSaveOptions(), true);
  doc.close(SaveOptions.DONOTSAVECHANGES);
}
function gen(prompt, out){
  var doc = app.documents.add(720, 1280, 72, "gen", NewDocumentMode.RGB, DocumentFill.WHITE);
  doc.selection.selectAll(); gfill(prompt); doc.flatten(); doc.selection.deselect();
  doc.saveAs(new File(out), new PNGSaveOptions(), true); doc.close(SaveOptions.DONOTSAVECHANGES);
}
