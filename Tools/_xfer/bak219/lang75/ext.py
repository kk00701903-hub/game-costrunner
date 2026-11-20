import re,os,json,sys
root='_CoastRun/Scripts'
S=r'"((?:[^"\\]|\\.)*)"'
lit=set(); interp=0; tot=0
pat_T=re.compile(r'\bLoc\.T\(\s*(\$?)'+S+r'\s*,\s*(\$?)'+S+r'\s*\)')
pat_T2=re.compile(r'\bLoc\.T\(')
pat_Tr=re.compile(r'\bLoc\.Tr\(\s*(\$?)'+S+r'\s*\)')
for dp,dn,fn in os.walk(root):
    for f in fn:
        if not f.endswith('.cs'): continue
        s=open(os.path.join(dp,f),encoding='utf-8',errors='ignore').read()
        tot+=len(pat_T2.findall(s))
        for m in pat_T.finditer(s):
            if m.group(3)=='$' : interp+=1; continue
            lit.add(m.group(4))
        for m in pat_Tr.finditer(s):
            if m.group(1)!='$': lit.add(m.group(2))
        if f in ('Loc.cs',):
            for m in re.finditer(r'\{ "[^"]+", '+S+r' \}',s): lit.add(m.group(1))
        if f=='ChapterScript.En.cs':
            for m in re.finditer(r'return '+S+';',s): lit.add(m.group(1))
            for m in re.finditer(r'\{ "[A-Z0-9_]+:\d+", '+S+r' \}',s): lit.add(m.group(1))
lit={x for x in lit if x.strip() and re.search('[A-Za-z]',x)}
res={'total_T':tot,'interp':interp,'lit':len(lit)}
for L in ('ja','es'):
    keys=set()
    for line in open(f'Resources/CoastRun/Lang/lang_{L}.txt',encoding='utf-8'):
        line=line.rstrip('\n').rstrip('\r'); t=line.find('\t')
        if t>0: keys.add(line[:t])
    # file keys use literal \n ; C# literal also uses \n escape -> compare escaped forms
    miss=sorted(x for x in lit if x not in keys)
    res[L]=len(miss); res[L+'_keys']=len(keys)
    json.dump(miss,open(f'/tmp/miss_{L}.json','w',encoding='utf-8'),ensure_ascii=False)
print(res)
