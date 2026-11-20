var C="clean Japanese anime cel-shaded game character illustration, bold even black outlines, flat bright colors with one shadow tone, crisp, full body head to toe, single character, isolated on plain pure white background, no floor, no ground shadow, no cast shadow, no text. Character: cute 10-year-old Korean girl with cute chibi proportions (large head, small body), SHORT straight black bob hair cut exactly at chin length (not long hair, no ponytail) with straight blunt bangs, two small yellow bar hair clips on one side of her head, big dark eyes, rosy cheeks";
var O=", wearing a yellow t-shirt, cuffed blue denim shorts, blue backpack, white sneakers with white socks";
function tee(c){return ", wearing a "+c+" t-shirt, cuffed blue denim shorts, blue backpack, white sneakers with white socks";}
var HOOD=", wearing a light blue hoodie, cuffed blue denim shorts, white sneakers, a small black boombox radio hanging on a shoulder strap";
var SUM=", wearing a light blue t-shirt, cuffed blue denim shorts, brown sandals, a straw hat hanging on her back";
var WIN=", wearing a long gray winter coat over a light blue hoodie, a knitted scarf, blue jeans, brown winter boots";
var P_N=". Pose: standing straight facing front, arms relaxed at her sides, gentle closed-mouth smile.";
var P_H=". Pose: standing facing front, both arms raised high with fists, cheering, big open-mouth happy smile, eyes sparkling.";
var P_T=". Pose: standing facing front, shoulders slumped, tired droopy half-closed eyes, small frown, faint dark circles under eyes.";
var T="tall", S="mid", Q="sq", WD="wide";
var SZ={tall:[1024,1792], mid:[1152,1536], sq:[1280,1280], wide:[1792,1024]};
var P={
 "Raise_Girl_Normal":[T,C+O+P_N],
 "Raise_Girl_Normal_AUTUMN":[T,C+tee("orange")+P_N],
 "Raise_Girl_Normal_NOON":[T,C+tee("mint green")+P_N],
 "Raise_Girl_Normal_WINTER":[T,C+tee("navy blue")+P_N],
 "Raise_Girl_Happy":[S,C+O+P_H],
 "Raise_Girl_Happy_AUTUMN":[S,C+tee("orange")+P_H],
 "Raise_Girl_Happy_NOON":[S,C+tee("mint green")+P_H],
 "Raise_Girl_Happy_WINTER":[S,C+tee("navy blue")+P_H],
 "Raise_Girl_Tired":[T,C+O+P_T],
 "Raise_Girl_Tired_AUTUMN":[T,C+tee("orange")+P_T],
 "Raise_Girl_Tired_NOON":[T,C+tee("mint green")+P_T],
 "Raise_Girl_Tired_WINTER":[T,C+tee("navy blue")+P_T],
 "Raise_Girl_Pose_Angry":[T,C+O+". Pose: standing, pouting angry face, puffed cheeks, furrowed brows, both fists clenched at chest height."],
 "Raise_Girl_Pose_Birthday":[T,C+O+". Pose: standing, holding a small strawberry birthday cake with lit candles in both hands, big happy smile."],
 "Raise_Girl_Pose_BusStop":[T,C+HOOD+". Pose: standing next to a short bus stop sign pole, leaning slightly, holding a small milk carton, calm waiting expression."],
 "Raise_Girl_Pose_Cafe":[T,C+", wearing a yellow t-shirt, brown cafe apron, cuffed denim shorts, white sneakers. Pose: standing, holding a wooden tray with two paper coffee cups, polite small smile."],
 "Raise_Girl_Pose_Cook":[T,C+", wearing a yellow t-shirt, white cooking apron, cuffed denim shorts, blue backpack, white sneakers. Pose: standing, holding a small steaming pot and a ladle, cheerful tongue-out smile."],
 "Raise_Girl_Pose_Cry":[T,C+O+". Pose: standing, crying, wiping tears from one eye with her hand, trembling lips."],
 "Raise_Girl_Pose_Dance":[S,C+O+", light blue headphones on her head. Pose: dancing on one leg, one finger pointing up, happy open mouth, small music notes around."],
 "Raise_Girl_Pose_Delivery":[T,C+O+". Pose: walking forward carrying a big cardboard box with both arms, determined small smile."],
 "Raise_Girl_Pose_Eat":[T,C+O+". Pose: standing, holding a red bowl of rice and chopsticks, eating happily with full cheeks."],
 "Raise_Girl_Pose_Garden":[Q,C+O+". Pose: crouching down, watering a small potted tomato plant with a small watering can, gentle smile, side three-quarter view."],
 "Raise_Girl_Pose_Haenyeo":[T,C+", diving goggles pushed up on her forehead, wearing a black long-sleeve rash guard shirt, an orange swim ring around her waist, cuffed denim shorts, white sneakers. Pose: standing, holding a small pink mesh net bag, curious smile."],
 "Raise_Girl_Pose_HugKid":[Q,C+O+". Pose: sitting on the grass giving a warm hug to her 4-year-old little brother who wears an orange raincoat, both smiling happily."],
 "Raise_Girl_Pose_Laugh":[T,C+O+". Pose: standing, laughing heartily with eyes squeezed shut, wide open mouth."],
 "Raise_Girl_Pose_Letter":[T,C+HOOD+". Pose: sitting with knees up, holding a white letter paper in both hands, one hand near her mouth, quiet surprised look."],
 "Raise_Girl_Pose_Marbles":[S,C+O+". Pose: squatting low, flicking a glass marble with one finger, winking playfully, a couple of marbles on the ground."],
 "Raise_Girl_Pose_Milk":[T,C+O+". Pose: standing, drinking from a white milk carton with both hands, eyes closed contentedly."],
 "Raise_Girl_Pose_NightWalk":[T,C+", wearing a light blue hoodie with a yellow reflective safety vest, cuffed denim shorts, white sneakers, a small black boombox radio on a shoulder strap. Pose: walking, holding a lit flashlight pointing down, alert look."],
 "Raise_Girl_Pose_Radio":[Q,C+HOOD+". Pose: sitting cross-legged on the ground, holding the small black boombox radio close to her ear with both hands, eyes closed, listening."],
 "Raise_Girl_Pose_Raincoat":[T,C+HOOD+". Pose: standing, hugging a folded bright orange child's raincoat to her chest, eyes closed, quiet face."],
 "Raise_Girl_Pose_Run":[S,C+O+". Pose: running fast to the left, side view, arms swinging, short bob hair bouncing, determined smile."],
 "Raise_Girl_Pose_Shy":[T,C+O+". Pose: standing, hands behind her back, shy blushing face, looking slightly down."],
 "Raise_Girl_Pose_Sick":[T,C+", wearing a yellow t-shirt and denim shorts, a small folded white towel on her forehead. Pose: standing wrapped in a big cozy white blanket held at the chest, she has a cold, pink cheeks, sleepy droopy eyes."],
 "Raise_Girl_Pose_Skate":[S,C+O+". Pose: riding a pink skateboard, one foot on board, arms spread out for balance, short bob hair, happy."],
 "Raise_Girl_Pose_Sleep":[T,C+O+". Pose: standing, dozing off, eyes closed, head tilted, sleepy."],
 "Raise_Girl_Pose_SleepLying":[WD,C+O+". Pose: lying on her side on the floor, fast asleep, eyes closed, small zzz above her head, full body horizontal."],
 "Raise_Girl_Pose_Snow":[T,C+WIN+". Pose: standing, holding a small snowball in both mittened hands, happy closed-eye smile."],
 "Raise_Girl_Pose_Surprised":[T,C+O+". Pose: standing, both hands on her cheeks, surprised wide eyes, small O-shaped mouth."],
 "Raise_Girl_Pose_Think":[T,C+O+". Pose: standing, thinking with one finger on her chin, looking up, a small yellow question mark floating above."],
 "Raise_Girl_Pose_Tower":[T,C+HOOD+". Pose: back view, standing and looking up into the distance, one hand shading her eyes."],
 "Raise_Girl_Pose_Wave":[T,C+O+". Pose: standing, waving one hand high in greeting, cheerful open-mouth smile."],
 "Raise_Girl_S1_Normal":[T,C+O+P_N],
 "Raise_Girl_S1_Happy":[S,C+O+P_H],
 "Raise_Girl_S1_Tired":[T,C+O+". Pose: standing, yawning with eyes closed, covering her mouth with one hand, sleepy."],
 "Raise_Girl_S2_Normal":[S,C+SUM+". Pose: standing, holding the straw hat brim at her side, gentle smile."],
 "Raise_Girl_S2_Happy":[S,C+SUM+". Pose: jumping happily, arms spread wide, eyes closed big smile."],
 "Raise_Girl_S2_Tired":[S,C+SUM+". Pose: standing in summer heat, wiping sweat from her forehead with the back of her hand, tired half-closed eyes."],
 "Raise_Girl_S3_Normal":[T,C+HOOD+P_N],
 "Raise_Girl_S3_Happy":[S,C+HOOD+". Pose: jumping with both arms raised high, eyes closed big happy smile."],
 "Raise_Girl_S3_Tired":[T,C+HOOD+". Pose: standing, sleepy, eyes closed, head drooping slightly."],
 "Raise_Girl_S4_Normal":[T,C+WIN+P_N],
 "Raise_Girl_S4_Happy":[S,C+WIN+". Pose: jumping with both arms raised high, big happy smile."],
 "Raise_Girl_S4_Tired":[T,C+WIN+". Pose: standing, both hands cupped at her mouth blowing warm breath, cold tired eyes."],
 "UI_Haneul_Stand":[S,C+O+". Pose: standing facing front, arms slightly open at her sides, friendly open smile."]
};
function genMany(list){ var out=[]; for(var i=0;i<list.length;i++){ var p=P[list[i]]; var sz=SZ[p[0]]; try{ out.push(genOne(list[i],sz[0],sz[1],p[1])); }catch(e){ out.push("ERR "+list[i]+": "+e); while(app.documents.length) app.activeDocument.close(SaveOptions.DONOTSAVECHANGES);} } return out.join("\n"); }
