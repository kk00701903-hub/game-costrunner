$.evalFile("C:/dev/game/Tools/Photoshop/gfill.jsx");
function icons213(start, n){
  var f=new File("C:/dev/game/Tools/_xfer/icons213/list.txt"); f.encoding="UTF-8"; f.open("r"); var lines=[]; while(!f.eof){var l=f.readln(); if(l.length>2) lines.push(l);} f.close();
  var done=[];
  var made=0; for (var i=start;i<lines.length && made<n;i++){
    var p=lines[i].split("|"); var out="C:/dev/game/Tools/_xfer/icons213/"+p[0]+".png";
    if (new File(out).exists) { done.push(p[0]+"(skip)"); continue; }
    try { gen("cute cartoon illustration of "+p[1]+", single game item icon centered on plain white background, animal crossing style game art", out); done.push(p[0]); made++; } catch(e){ try{ while(app.documents.length>0) app.documents[0].close(SaveOptions.DONOTSAVECHANGES);}catch(e2){} done.push(p[0]+"!ERR "+e); break; }
  }
  return done.join(",");
}
