# 컷씬 대본 v3 (77차, 2026-09-13)

전편 재작성: 오프닝 11컷 × 10.6초(≈1:57), 컷씬 8편 각 12컷 × 8.5초(≈1:42), 엔딩 3편 각 8컷 × 8.5초(≈1:08). 카드는 제목만(CHAPTER 표기 삭제). 그림 파일은 `Cut_S_<컷id>.jpg`(Resources/CoastRun, 720×1280) — **131장 전부 Kling 웹(IMAGE 3.0, 1K)으로 생성해 적용(9/13)**. 원본 768×1360 PNG 는 `Tools/KlingGen/out/web77/`. 없으면 옛 스틸(fallback)로 재생.

생성 메모: 앵커 이미지 참조(`reference_type=subject`)는 구도를 깨므로 바이블 문장만으로 생성. 검수에서 거꾸로 반사·액자 테두리·글자·거대 손이 나온 8장(OP_5, N3_05, N3_09, N4_02, N4_11, N5_03, N7_03, EB_03)은 장면 문장을 「wide/mid shot, natural proportions, no inner frame, no letters」로 고쳐 재생성. 손 클로즈업은 피하고 인물 중간샷으로 쓰는 편이 안전.

## 캐릭터 바이블 (Kling 앵커 = Tools/KlingGen/ref77/<키>.png)

| 키 | 인물 | 나이 | 외모·옷(고정) |
|---|---|---|---|
| H19 | 하늘(현재) | 19 | 어깨 아래 긴 검은 생머리 + 작은 노란 머리핀 · **하늘색 후드티** · 연청 반바지 · 흰 운동화 (주황은 절대 안 입음 — 우비는 꼬마만) |
| H12 | 하늘(어린 시절) | 12 | 턱선 단발 + 노란 머리핀 · 동그란 얼굴 · **바랜 연하늘 티셔츠** · 회색 반바지 · 낡은 샌들 |
| D12 | 도윤(어린 시절) | 12 | 아주 창백 · 단정한 짧은 검은 머리 · **흰 셔츠 + 남색 니트 조끼** · 회색 반바지 · 검은 구두(서울 부잣집) |
| D20 | 도윤(현재) | 20 | 키 큼 · 창백 · 부드러운 검은 머리 · **회색 후드 코트 + 남색 후드** · 진청 바지 · 흰 운동화 · 왼 손목 흰 병원 팔찌 |
| KID | 꼬마 「바다」 | 6 | **너무 큰 주황 우비**(후드 쓴 채, 발목까지) · 후드 아래 동그란 뺨·큰 눈이 보임 · 노란 장화 |
| DAD | 아빠 | 40대 | 그을린 어부 · 흰머리 섞인 짧은 머리 · 수염 · **같은 주황 우비** · 검은 장화 |
| MOM | 엄마 | **마흔다섯(중년, 노인 아님)** | 해녀 · **검은 네오프렌 해녀복 + 검은 두건** · 이마에 올린 물안경 · **주황 태왁**(둥근 박 모양 부표 + 망사리, 튜브 아님) · 둥글고 볕에 그은 얼굴, 통통하고 다부진 체격, 검은 머리에 흰머리 몇 올 — **깊은 주름·백발·할머니 금지**(80차: 프롬프트에 `only FORTY-FIVE years old … must NOT look elderly` 를 반드시 넣을 것) |
| GUARD | 보디가드 | — | 검은 양복 · 선글라스 · 이어피스 |
| BOAT | 아빠 배 | — | **작은 연안 고기잡이 배**: 흰·파랑 선체, 작은 조타실, 마스트 등, 그물과 주황 부표(나룻배 아님) |

**화풍 프롬프트(컷씬 7 기준)**: `Clean soft anime illustration in Korean webtoon key-visual style, delicate linework, soft gradient shading, luminous atmospheric lighting, detailed painterly background, gentle pastel-dusk palette, full-bleed vertical 9:16 composition, no border, no frame, no text.`

**부정 프롬프트**: `watercolor paper texture, border, frame, white edge, text, watermark, photo, 3d render, extra fingers, deformed hands` (+ 어린이 컷: `adult, teenager, grown man`)

## 80차 개정 — 반전 순서(사용자 지시)
- **18화(컷씬 7)까지 「여주가 죽었다」를 확정 짓지 않는다.** 대신 독자가 **도윤이 죽은 것 같다**고 의심하게 둔다: 새벽의 약 한 움큼과 끊이지 않는 병원 팔찌(7-3 「이 사람, 얼마나 남은 걸까」), 이름 없는 국화를 「그를 위한 꽃」으로 읽는 하늘(7-10), 유리에 비친 한 사람을 자기라고 믿는 장면(7-12).
- 그래서 **유령 확증 묘사를 걷어냈다**: 1-4 사람들이 「스치고 지나감」 → 인사에 대답이 없음 / 3-4 손이 「그냥 지나감」 → 잡으려다 멈춤 / 3-6 「내 발자국이 없다」 → 돌아보면 눈에 덮여 있음 / 7-5·7-6 「통과」 → 눈을 맞추지 않음 / 6-11 「살아 돌아온 줄로만 알았다」 추가.
- **트럭 컷(5-3)**: 「트럭이 나를 그대로 통과」 → **「트럭이 바로 옆을 스쳤고 나는 길 옆으로 넘어졌다」** + 그림도 재생성(옆으로 지나가는 트럭, 갓길에 넘어진 하늘, 밀어낸 꼬마의 찢어진 소매).
- **꼬마 = 기억을 가진 환생한 아빠**는 20화(컷씬 8)에서 긴가민가하게: 8-8 「여섯 살이라던 아이가 여덟 해 전 그 밤을 알고 있었다」, 8-9 꼬마의 말버릇 「한 마리만 더 잡으민 되주」(아빠의 말). 확정은 엔딩 B(「아빠 맞지」)에서.
- 잔손: 엔딩 A 5컷의 「스무 챕터 만에」 → 「여덟 해 만에」, 5-6 「심장이었다」 → 「심장병이었다」.

## 떡밥·복선 체크리스트
- 오프닝 2·3·5·6: 괴롭힘(도시락 없음·생선 냄새·장날 좌판·가방을 도랑에) → 도윤이 가방을 건지고 앞에 서 준 뒤 평화(「세상이 조용했다」).
- 컷씬 3-10: 괴롭히던 아이들이 다시 왔을 때 도윤이 막아섬(「우리 집 아저씨가 온다」).
- 컷씬 1-8 · 2-12 · 4-10 · 8-9: 탑 아래 돌 세 개(도윤이 쌓음 — 너·나·아저씨) → 엔딩에서 꼬마가 넷째 돌을 얹거나 무너뜨림.
- 컷씬 2-7·8·11: 아빠의 고기잡이 배와 하트 두 개 부표 → 뒤집힌 배 옆에 부표만.
- 컷씬 6-8·9: 해녀 출근 아침마다 벽의 주황 우비가 떨어짐(못은 멀쩡) → 세 번째 아침 우비를 안고 나간 게 마지막(아빠의 경고).
- 컷씬 7-10·11: 도윤 방의 이름 없는 국화, 꼬마의 「저 사람은 아직 몰라」.
- 컷씬 8-9: 탑 아래 개켜진 찢어진 우비(꼬마 = 아빠).


## OPEN 「너와 나의 주파수」 · BGM_M5 · 11컷 × 10.6s · 채도 1.0

1. 열두 살 봄. 제주의 작은 마을, 언덕 위엔 송전탑이 서 있었다. 나는 그 아래서 혼자 노는 아이였다.  
   `Cut_S_OP_1` (앵커 H12 · 폴백 `Cut_V_OP_1`)  
   _wide shot, spring rapeseed field on a Jeju hill under a tall steel transmission tower, a small 12-year-old girl HANEUL sitting alone in the yellow flowers with her back to the camera, sea behind_
2. **— 회상 · 열두 살 —** 학교에선 늘 혼자였다. 해녀 엄마의 생선 냄새가 난다고 아이들은 내 옆에 앉지 않았다.  
   `Cut_S_OP_2` (앵커 H12 · 폴백 `Cut_V_N3_1`)  
   _elementary school classroom at lunch, 12-year-old HANEUL sitting alone at a desk with no lunchbox, other children at far desks turned away whispering and holding their noses, harsh noon light, HANEUL looking down at her hands_
3. **— 회상 · 열두 살 —** 장날엔 좌판 옆에 앉아 생선을 팔았다. 같은 반 아이들이 지나가며 웃었다. 나는 고개를 들지 않았다.  
   `Cut_S_OP_3` (앵커 H12 · 폴백 `Cut_V_N2_2`)  
   _Jeju market day, 12-year-old HANEUL crouching beside her mother's fish stall (mother in plain work clothes and a headscarf) on the street, three same-age children passing by pointing and laughing, HANEUL hiding her face behind her knees, morning light_
4. 서울서 온 창백한 전학생, 도윤. 항상 검은 양복 아저씨가 뒤에 서 있는 이상한 애. 그 애가 먼저 손을 내밀었다.  
   `Cut_S_OP_4` (앵커 D12 · 폴백 `Cut_V_OP_2`)  
   _school gate at spring, 12-year-old DOYUN in white shirt and navy vest holding out his hand to 12-year-old HANEUL, a tall bodyguard in a black suit standing a few steps behind him, cherry blossom petals, HANEUL surprised_
5. 아이들이 내 가방을 도랑에 던진 날, 그 애가 도랑에 들어가 가방을 건졌다. 흰 셔츠가 흙투성이가 됐는데도 웃었다.  
   `Cut_S_OP_5` (앵커 D12 · 폴백 `Cut_V_OP_5`)  
   _12-year-old DOYUN standing knee-deep in a muddy roadside ditch holding up HANEUL's dripping old backpack, his white shirt and navy vest smeared with mud, smiling up at 12-year-old HANEUL on the road, the bullies running away in the background_
6. 그날부터 아무도 나를 건드리지 않았다. 그 애 옆에 있으면 세상이 조용했다. 처음으로 학교가 무섭지 않았다.  
   `Cut_S_OP_6` (앵커 H12 · 폴백 `Cut_V_N3_1`)  
   _quiet classroom, 12-year-old HANEUL and 12-year-old DOYUN sitting side by side at one desk sharing a lunchbox, soft afternoon light through the window, the other children in the background no longer looking, peaceful_
7. 유리구슬 한 알에 온 세상을 걸던 나이. 탑 아래 움푹한 자리가 우리 기지였다.  
   `Cut_S_OP_7` (앵커 H12 · 폴백 `Cut_V_OP_3`)  
   _two 12-year-old children HANEUL and DOYUN crouching in a grassy hollow under the steel transmission tower playing with glass marbles, an old blanket and a broken radio beside them, golden afternoon_
8. 그 애는 늘 내 앞에 섰다. 한 번도 이유를 말하지 않고. 나는 그 등만 보고 걸었다.  
   `Cut_S_OP_8` (앵커 D12 · 폴백 `Cut_V_OP_5`)  
   _coastal road at dusk, 12-year-old DOYUN walking ahead with his back to the camera, 12-year-old HANEUL following a few steps behind looking at his back, steel tower far ahead, long shadows_
9. 떠나던 날 그 애가 말했다. 스무 살 네 생일, 송전탑 밑에서 기다릴게. 검은 차가 시동을 건 채 서 있었다.  
   `Cut_S_OP_9` (앵커 D12 · 폴백 `Cut_V_OP_7`)  
   _a black sedan with its door open on a coastal road, 12-year-old DOYUN leaning out to speak to 12-year-old HANEUL who stands on the roadside, the bodyguard holding the door, grey overcast sky, steel tower on the hill behind_
10. 열아홉의 어느 아침, 눈을 떴다. 낯선 방. 내 이름도, 어제도 떠오르지 않았다.  
   `Cut_S_OP_10` (앵커 H19 · 폴백 `Cut_V_OP_8`)  
   _19-year-old HANEUL waking up in a dim old Jeju room, sitting up in bed in her sky-blue hoodie touching her head, morning light through a small window, confused_
11. 벽의 달력엔 한 줄뿐. 스무 살 생일, 송전탑 아래. 1년 남았다. 그 약속 하나가 나를 여기 붙들어 두고 있다.  
   `Cut_S_OP_11` (앵커 H19 · 폴백 `Cut_V_OP_9`)  
   _close-up of an old wall calendar with a single date circled in pencil and a tiny heart drawn beside it, 19-year-old HANEUL's hand touching the page, a steel transmission tower visible through the window beside it_

## CS1 「이름」 · BGM_M1 · 12컷 × 8.5s · 채도 0.85

1. 눈을 떴다. 낯선 방, 낯선 아침. 문가에 주황 우비를 뒤집어쓴 꼬마가 서 있었다. 후드 아래로 동그란 뺨이 보였다.  
   `Cut_S_N1_01` (앵커 KID · 폴백 `Cut_V_CH01_Open`)  
   _a dim old Jeju room in the morning, a tiny child in an oversized orange rain coat standing in the doorway with the hood up, round cheeks and big eyes clearly visible under the hood, looking at 19-year-old HANEUL who sits up in bed wearing a sky-blue hoodie_
2. 내 이름이 뭐였더라. 어디서 왔는지, 왜 여기 누워 있었는지, 아무것도 떠오르지 않았다.  
   `Cut_S_N1_02` (앵커 H19 · 폴백 `Cut_V_N1_1`)  
   _19-year-old HANEUL in a sky-blue hoodie standing in front of a foggy old mirror touching her own cheek, lost, soft window light, a small orange rain coat hanging on a hook by the door behind her_
3. 벽의 달력엔 딱 한 줄. 1년 뒤 내 생일에 동그라미. 나머지 칸은 전부 하얗게 비어 있었다.  
   `Cut_S_N1_03` (앵커 H19 · 폴백 `Cut_CS1_2`)  
   _old wall calendar with one pencil circle around a date, all other days blank, 19-year-old HANEUL's hand resting on the wall beside it, dust in the light_
4. 창밖 마을 사람들은 나를 스치고도 돌아보지 않았다. 거울은 아무리 닦아도 뿌옇기만 했다.  
   `Cut_S_N1_04` (앵커 H19 · 폴백 `Cut_V_CH03_Open`)  
   _Jeju village lane with black basalt stone walls, 19-year-old HANEUL standing still in her sky-blue hoodie while two villagers walk straight past her without looking, morning haze_
5. 이 꼬마만 나를 봤다. 이름이 없다고 하니, 바다처럼 파랗다며 「바다」라고 지어 줬다.  
   `Cut_S_N1_05` (앵커 KID · 폴백 `Cut_V_CH17_Open`)  
   _19-year-old HANEUL in a sky-blue hoodie crouching to eye level with the tiny child in the oversized orange rain coat, the child pointing at her blue hoodie and grinning, cheeks visible under the hood, sea behind them_
6. 정류장에서 버스를 기다렸지만 오지 않았다. 꼬마가 멀리 송전탑을 가리켰다. 저기까지 가 보자고.  
   `Cut_S_N1_06` (앵커 H19 · 폴백 `Cut_V_N1_2`)  
   _stone bus shelter on a Jeju coastal road, 19-year-old HANEUL in a sky-blue hoodie waiting, the tiny child in the orange rain coat pointing far away at a steel transmission tower on a hill, overcast_
7. 탑까지는 걸어서 한참. 꼬마는 한 번도 뒤돌아보지 않고 앞장서 걸었다. 나는 그 주황색만 따라갔다.  
   `Cut_S_N1_07` (앵커 KID · 폴백 `Cut_V_CH06_Open`)  
   _long coastal road toward a steel transmission tower, the tiny child in the orange rain coat walking ahead, 19-year-old HANEUL in a sky-blue hoodie following, rapeseed and stone walls, soft light_
8. 탑 아래 돌 세 개가 쌓여 있었다. 꼬마가 말했다. 「이건 건드리면 안 돼.」 누가 쌓았는지는 말해 주지 않았다.  
   `Cut_S_N1_08` (앵커 KID · 폴백 `Cut_V_CH12_Close`)  
   _close-up at the foot of the steel transmission tower: three small stones stacked in a cairn on the grass, the tiny child in the orange rain coat kneeling beside them protectively, 19-year-old HANEUL's sneakers at the edge of frame_
9. 집 문 앞에 스케이트보드 하나가 놓여 있었다. 누가 갖다 놓았는지는 묻지 않았다. 발이 먼저 올라섰다.  
   `Cut_S_N1_09` (앵커 H19 · 폴백 `Cut_V_CH19_Close`)  
   _a pink skateboard leaning against the wooden gate of an old Jeju stone house, 19-year-old HANEUL in a sky-blue hoodie putting one white sneaker on it, morning_
10. 보드에 올라서자 몸이 길을 기억했다. 바람, 돌담, 바다 냄새. 처음인데 처음이 아니었다.  
   `Cut_S_N1_10` (앵커 H19 · 폴백 `Cut_V_N1_3`)  
   _19-year-old HANEUL in a sky-blue hoodie riding a skateboard along a coastal road between black basalt walls, hair blown back, sea and a steel tower ahead, sense of speed_
11. 노을 속 탑 아래 우유 두 병이 놓여 있었다. 하나는 늘 그대로였다. 누가, 누구를 위해 두고 가는 걸까.  
   `Cut_S_N1_11` (앵커 H19 · 폴백 `Cut_V_CH14_Close`)  
   _two glass milk bottles standing on a flat rock at the foot of the steel transmission tower at sunset, one full and one empty, 19-year-old HANEUL crouching to look at them_
12. 돌아오는 길, 꼬마가 내 손을 잡았다. 작고 차가운 손. 「내일도 가자.」 나는 고개를 끄덕였다.  
   `Cut_S_N1_12` (앵커 KID · 폴백 `Cut_V_CH06_Open`)  
   _dusk coastal road, the tiny child in the orange rain coat holding the hand of 19-year-old HANEUL in a sky-blue hoodie, both seen from behind walking home, the tower glowing with a red lamp behind them_

## CS2 「하트」 · BGM_M6 · 12컷 × 8.5s · 채도 0.78

1. 빨랫줄엔 해녀복 두 벌. 우편함엔 뜯지 않은 편지가 스무 통. 이 집엔 나 말고 누가 살았던 걸까.  
   `Cut_S_N2_01` (앵커 H19 · 폴백 `Cut_V_CH02_Open`)  
   _yard of an old Jeju stone house, two black haenyeo wetsuits hanging on a clothesline, a rusty red mailbox stuffed with unopened letters, 19-year-old HANEUL in a sky-blue hoodie standing in the yard looking at them_
2. 편지 겉봉의 이름은 읽을 수가 없었다. 뜯으려 하면 손이 떨렸다. 무서운 게 무엇인지도 모른 채.  
   `Cut_S_N2_02` (앵커 H19 · 폴백 `Cut_V_N2_1`)  
   _close-up of 19-year-old HANEUL's trembling hands holding an unopened envelope, the handwritten name blurred and unreadable, sky-blue hoodie sleeves, yellow flowers behind_
3. 바람이 편지 한 통을 채 갔다. 쫓아가다 처음으로 탑의 쇠기둥에 손이 닿았다. 차갑고, 웅웅 울렸다.  
   `Cut_S_N2_03` (앵커 H19 · 폴백 `Cut_V_CH09_Close`)  
   _19-year-old HANEUL in a sky-blue hoodie pressing her palm against the cold steel leg of the transmission tower, a letter caught on the grass at her feet, wind, faint glow around her hand_
4. **— 회상 —** 숨비소리. 물 위로 올라온 해녀가 길게 내쉬는 숨. 엄마의 첫 조각이 그 소리로 돌아왔다.  
   `Cut_S_N2_04` (앵커 MOM · 폴백 `Cut_CS2_3`)  
   _a haenyeo diver's head breaking the sea surface exhaling a long whistling breath, black wetsuit hood and goggles, an orange tewak float bobbing beside her, morning sea_
5. **— 회상 · 열세 해 전 —** 엄마는 해녀였다. 나는 바닷가 바위에서 엄마 숨소리를 세며 기다리는 아이였다. 그것만은 확실했다.  
   `Cut_S_N2_05` (앵커 H12 · 폴백 `Cut_V_N2_2`)  
   _12-year-old HANEUL in a faded pale-blue t-shirt sitting on black rocks by the sea, her haenyeo mother in a black wetsuit with an orange tewak float far out in the water, morning_
6. 도윤. 그 애 이름만 먼저 돌아왔다. 길에서 불러 봤지만 그 뒷모습은 한 번 멈칫하고 그냥 갔다.  
   `Cut_S_N2_06` (앵커 D20 · 폴백 `Cut_V_CH16_Close`)  
   _coastal road, 20-year-old DOYUN in a grey hooded coat walking away with his back to the camera, 19-year-old HANEUL in a sky-blue hoodie calling out with her hand raised, he does not turn_
7. **— 회상 · 열세 해 전 —** 아빠는 어부였다. 작은 고기잡이 배 한 척이 전부였다. 그릴 줄 아는 건 하트 하나뿐이라 부표마다 빨간 하트를 그렸다.  
   `Cut_S_N2_07` (앵커 DAD · 폴백 `Cut_V_CH04_Open`)  
   _HANEUL's father, a weathered fisherman in an orange rain coat, painting a red heart on a white buoy on the deck of his small white-and-blue fishing boat with a wheelhouse, nets piled behind him, 12-year-old HANEUL watching from the pier_
8. **— 회상 · 열세 해 전 —** 내 부표엔 하트가 두 개였다. 하나는 나, 하나는 아빠. 아빠는 그걸 뱃머리에 매달았다.  
   `Cut_S_N2_08` (앵커 DAD · 폴백 `Cut_V_CH08_Open`)  
   _close-up of a white buoy with two red painted hearts hanging from the bow of a small fishing boat, the fisherman's weathered hand tying the rope, harbor light_
9. **— 회상 · 태풍 전날 밤 —** 생일 전날 밤, 태풍 예보. 한 마리만 더 잡고 올게. 아빠는 주황 우비를 입고 나갔다. 나는 소매를 놓았다.  
   `Cut_S_N2_09` (앵커 DAD · 폴백 `Cut_V_N2_3`)  
   _rainy night in a Jeju stone-house yard, the fisherman father in an orange rain coat with the hood up turning toward the gate, 12-year-old HANEUL in a pale-blue t-shirt letting go of his sleeve, a dim porch lamp_
10. **— 회상 · 태풍 전날 밤 —** 그날 밤 비는 담을 넘도록 내렸다. 나는 대문 앞에서 밤새 기다렸다. 우비는 돌아오지 않았다.  
   `Cut_S_N2_10` (앵커 H12 · 폴백 `Cut_V_CH13_Open`)  
   _12-year-old HANEUL in a pale-blue t-shirt sitting alone under the eaves of a wooden gate in pouring night rain, hugging her knees, the lane empty, a single lamp_
11. 다음 날은 잔인하게 맑았다. 아빠 배는 탑 아래 바다에서 뒤집힌 채 발견됐다. 하트 부표만 떠 있었다.  
   `Cut_S_N2_11` (앵커 - · 폴백 `Cut_V_CH13_Close`)  
   _bright clear morning after the storm, a small white-and-blue fishing boat capsized on the rocks below the transmission tower cliff, a white buoy with two red hearts floating on the water, villagers on the cliff edge_
12. 그래서 탑이 무섭고, 그리웠다. 오늘도 탑 아래엔 돌 세 개가 그대로 있었다. 누군가 매일 다녀가는 것처럼.  
   `Cut_S_N2_12` (앵커 H19 · 폴백 `Cut_V_CH12_Close`)  
   _19-year-old HANEUL in a sky-blue hoodie standing at the foot of the steel transmission tower at dusk looking up, three stacked stones at her feet, the sea below_

## CS3 「우리 기지」 · BGM_M2 · 12컷 × 8.5s · 채도 0.7

1. 부엌 창가의 낡은 도시락. 계란말이가 전부 하트 모양이었다. 뚜껑 안쪽엔 열두 살 글씨 — 반은 네 거.  
   `Cut_S_N3_01` (앵커 H19 · 폴백 `Cut_V_CH05_Open`)  
   _an old tin lunchbox open on a kitchen windowsill, heart-shaped egg rolls inside, childish handwriting scratched into the lid, 19-year-old HANEUL's hands holding it, warm window light_
2. **— 회상 · 여덟 해 전 —** 열두 살의 나는 도시락이 없는 아이였다. 점심시간이면 운동장 구석에서 물만 마셨다.  
   `Cut_S_N3_02` (앵커 H12 · 폴백 `Cut_V_N3_1`)  
   _12-year-old HANEUL in a faded pale-blue t-shirt sitting alone on the edge of an empty school playground at lunch time drinking from a water tap, other children eating together far away_
3. **— 회상 · 여덟 해 전 —** 그 애가 자기 도시락을 반으로 가르며 말했다. 반은 네 거. 나는 처음으로 남 앞에서 밥을 먹었다.  
   `Cut_S_N3_03` (앵커 D12 · 폴백 `Cut_V_N3_1`)  
   _school classroom, 12-year-old DOYUN in white shirt and navy vest sliding half of his lunchbox across the desk to 12-year-old HANEUL, heart-shaped egg rolls, HANEUL hesitating with her hands in her lap_
4. 밤의 탑 아래 그가 혼자 앉아 있었다. 다가가 어깨를 잡으려던 손이 그냥 지나갔다. 그는 몰랐다.  
   `Cut_S_N3_04` (앵커 D20 · 폴백 `Cut_V_CH18_Mid`)  
   _night under the steel transmission tower, 20-year-old DOYUN in a grey hooded coat sitting on a rock, 19-year-old HANEUL's hand reaching for his shoulder and passing through it like mist, stars_
5. 첫눈 오는 날, 꼬마와 창가에서 성에에 얼굴을 그렸다. 둘을 그리다 손이 멈췄다. 나머지 하나가 누군지 몰라서.  
   `Cut_S_N3_05` (앵커 KID · 폴백 `Cut_V_CH17_Open`)  
   _frosted window with two smiling faces drawn in the frost by a finger, the tiny child in the orange rain coat and 19-year-old HANEUL in a sky-blue hoodie at the window, first snow falling outside_
6. 눈 위 발자국은 한 줄뿐이었다. 꼬마 것만. 나란히 걸었는데 내 발자국은 어디에도 없었다.  
   `Cut_S_N3_06` (앵커 KID · 폴백 `Cut_V_CH07_Open`)  
   _snowy coastal road seen from behind, a single line of tiny boot prints in the snow beside untouched snow, the tiny child in the orange rain coat and 19-year-old HANEUL walking side by side ahead_
7. **— 회상 · 여덟 해 전 —** 탑이 열두 살의 봄을 돌려줬다. 유채밭에서 그 애가 말했다. 내가 하는 말은 반은 진짜고 반은 거짓말이야.  
   `Cut_S_N3_07` (앵커 D12 · 폴백 `Cut_V_CH05_Close`)  
   _12-year-old DOYUN in white shirt and navy vest and 12-year-old HANEUL in a pale-blue t-shirt standing face to face in a yellow rapeseed field under the tower, spring wind, he is smiling mischievously_
8. **— 회상 · 여덟 해 전 —** 탑 아래 움푹한 자리가 우리 기지였다. 담요 한 장, 고장 난 라디오, 유리구슬. 세상에서 제일 안전한 곳.  
   `Cut_S_N3_08` (앵커 H12 · 폴백 `Cut_V_N3_2`)  
   _a children's secret base in a grassy hollow under the steel transmission tower: an old blanket, a broken radio, glass marbles, a wooden sign, 12-year-old HANEUL and 12-year-old DOYUN sitting inside, warm afternoon_
9. **— 회상 · 여덟 해 전 —** 정류장에서 탑까지 4.2킬로. 노을이 지기 전에 닿으면 내가 이기는 놀이였다. 매번 그 애가 져 줬다.  
   `Cut_S_N3_09` (앵커 H12 · 폴백 `Cut_V_Open_3`)  
   _two 12-year-old children HANEUL and DOYUN running along a coastal road toward the steel transmission tower at sunset, HANEUL slightly ahead laughing, DOYUN behind pretending to be out of breath_
10. **— 회상 · 여덟 해 전 —** 괴롭히던 아이들이 다시 왔을 때, 그 애는 내 앞에 서서 말했다. 얘 건드리면 우리 집 아저씨가 온다. 아이들은 물러났다.  
   `Cut_S_N3_10` (앵커 D12 · 폴백 `Cut_V_OP_5`)  
   _school back gate, 12-year-old DOYUN in white shirt and navy vest standing protectively in front of 12-year-old HANEUL facing three bullies, the tall bodyguard in a black suit visible at the far end of the lane, the bullies backing off_
11. 지금의 기지엔 라디오만 남아 있었다. 다이얼을 돌리자 잡음 사이로 91.9. 우리 주파수였다.  
   `Cut_S_N3_11` (앵커 H19 · 폴백 `Cut_V_N3_3`)  
   _19-year-old HANEUL in a sky-blue hoodie kneeling in the grassy hollow under the tower holding an old dusty radio with a glowing dial, dusk, rust and grass_
12. 그 애 말대로 하기로 했다. 지워지면 또 그리면 돼. 내일도 탑까지 달린다. 기억이 그 길 위에 있으니까.  
   `Cut_S_N3_12` (앵커 H19 · 폴백 `Cut_V_CH14_Open`)  
   _19-year-old HANEUL in a sky-blue hoodie standing on the coastal road at dawn with her skateboard, looking toward the distant steel tower, determined, soft pink sky_

## CS4 「열두 개의 초」 · BGM_M3 · 12컷 × 8.5s · 채도 0.6

1. 절벽 아래, 아빠 부표가 있던 바위. 누군가 새로 하트를 칠해 놓았다. 페인트 냄새가 아직 났다.  
   `Cut_S_N4_01` (앵커 H19 · 폴백 `Cut_V_CH08_Open`)  
   _black rocks below a cliff with a steel transmission tower above, a white buoy with a freshly painted red heart resting on the rock, sea spray, 19-year-old HANEUL in a sky-blue hoodie climbing down toward it_
2. 손끝에 빨간 페인트가 묻어났다. 아빠는 없는데 누가 아빠의 하트를 그리는 걸까. 꼬마는 대답하지 않았다.  
   `Cut_S_N4_02` (앵커 KID · 폴백 `Cut_V_N4_1`)  
   _close-up of 19-year-old HANEUL's fingertip with fresh red paint on it, the heart buoy behind, the tiny child in the orange rain coat looking away at the sea, puzzled mood_
3. 3킬로 지점에서 처음으로 주저앉았다. 다리가 아니라 가슴이 무거웠다. 돌아와 보니 부표의 하트가 지워져 있었다.  
   `Cut_S_N4_03` (앵커 H19 · 폴백 `Cut_V_CH09_Open`)  
   _19-year-old HANEUL in a sky-blue hoodie sitting collapsed on the roadside with her skateboard beside her, head down, a kilometer marker stone reading nothing, grey sky, the tower far away_
4. 돌 위에 초 하나가 켜져 있었다. 내가 다가가자 초는 저절로 꺼졌다. 바람은 없었다.  
   `Cut_S_N4_04` (앵커 H19 · 폴백 `Cut_CS4_6`)  
   _a single lit candle standing on a flat stone at the foot of the transmission tower at blue dusk, its flame going out with a thread of smoke, 19-year-old HANEUL's shadow approaching, no wind, grass still_
5. **— 회상 · 여덟 해 전 —** 열두 살 생일. 그 애는 초 열두 개를 꽂은 케이크를 들고 탑 아래로 나를 불렀다. 여기가 어떤 자리인지 모르고.  
   `Cut_S_N4_05` (앵커 D12 · 폴백 `Cut_V_CH10_Open`)  
   _night under the steel transmission tower, 12-year-old DOYUN in white shirt and navy vest holding a small birthday cake with twelve lit candles, smiling, 12-year-old HANEUL in a pale-blue t-shirt standing frozen a few steps away_
6. **— 회상 · 여덟 해 전 —** 나는 케이크를 엎었다. 여기서 아빠가 죽었어. 그 애는 그날 처음으로 내 앞에서 울었다.  
   `Cut_S_N4_06` (앵커 H12 · 폴백 `Cut_V_N4_2`)  
   _night under the tower, a birthday cake knocked over on the grass with candles scattered and smoking, 12-year-old HANEUL in a pale-blue t-shirt shouting with tears, 12-year-old DOYUN in a navy vest crying silently_
7. **— 회상 · 열흘 뒤 —** 그 뒤 열흘, 그 애는 오지 않았다. 담장 위에 두고 간 도시락은 그대로 얼어 있었다. 나도 열지 않았다.  
   `Cut_S_N4_07` (앵커 H12 · 폴백 `Cut_V_CH17_Close`)  
   _a tin lunchbox left on top of a black basalt stone wall covered in frost, 12-year-old HANEUL in a pale-blue t-shirt and a thin cardigan looking at it from the yard without touching it, cold grey morning_
8. **— 회상 · 열흘 뒤 —** 열흘째 되던 날, 그 애가 탑으로 뛰어왔다. 미안하다는 말 대신 숨을 헐떡이며 내 옆에 섰다. 그걸로 됐다.  
   `Cut_S_N4_08` (앵커 D12 · 폴백 `Cut_V_N4_3`)  
   _12-year-old DOYUN in white shirt and navy vest running through light rain toward the steel transmission tower where 12-year-old HANEUL stands waiting, he is out of breath, she turns toward him_
9. **— 회상 · 여덟 해 전 —** 그 집 아빠가 죽은 자리라고들 했다. 마을 사람들은 그렇게 불렀다. 그 애만 「우리 기지」라고 불렀다.  
   `Cut_S_N4_09` (앵커 D12 · 폴백 `Cut_V_CH12_Close`)  
   _village elders in the distance pointing up at the transmission tower on the cliff and whispering, in the foreground 12-year-old DOYUN and 12-year-old HANEUL sitting on the stone wall with their backs to them, overcast_
10. **— 회상 · 여덟 해 전 —** 기지에 돌 세 개를 쌓은 건 그 애였다. 하나는 너, 하나는 나, 하나는 아저씨. 무너지면 다시 쌓으면 돼.  
   `Cut_S_N4_10` (앵커 H12 · 폴백 `Cut_V_N3_2`)  
   _close-up of small hands stacking three stones into a cairn on the grass under the tower, 12-year-old DOYUN and 12-year-old HANEUL kneeling together, golden light_
11. 비가 시작됐다. 탑 아래엔 오늘도 아무도 없었다. 그래도 간다. 초를 켜 놓는 사람을 만나야 하니까.  
   `Cut_S_N4_11` (앵커 H19 · 폴백 `Cut_V_CH11_Open`)  
   _rain beginning on the coastal road, 19-year-old HANEUL in a sky-blue hoodie with the hood up walking toward the steel tower alone, wet asphalt reflecting the sky_
12. 꼬마가 뒤에서 뛰어와 우비 자락으로 내 머리를 덮어 줬다. 우비는 너무 커서 둘이 들어가고도 남았다.  
   `Cut_S_N4_12` (앵커 KID · 폴백 `Cut_V_N5_1`)  
   _rainy coastal road, the tiny child in the oversized orange rain coat lifting the coat's hem over the head of 19-year-old HANEUL in a sky-blue hoodie so they both fit under it, both laughing, seen from the front_

## CS5 「그 밤」 · BGM_M6 · 12컷 × 8.5s · 채도 0.5

1. 길가에 버려진 리어카 한 대. 바퀴 하나가 없었다. 왜 거기 있는지 그땐 몰랐다.  
   `Cut_S_N5_01` (앵커 H19 · 폴백 `Cut_V_CH12_Open`)  
   _an old wooden handcart abandoned in the grass beside a Jeju coastal road, one wheel missing, 19-year-old HANEUL in a sky-blue hoodie standing beside it looking at it, grey afternoon_
2. 빗속, 우산도 없이 탑 아래 선 뒷모습. 뛰어가면 사라지고, 멈추면 다시 있었다.  
   `Cut_S_N5_02` (앵커 D20 · 폴백 `Cut_CS5_2`)  
   _heavy rain, a lone figure of 20-year-old DOYUN in a grey hooded coat standing under the steel transmission tower seen from far behind, 19-year-old HANEUL's blurred back in the foreground running toward him_
3. 트럭이 나를 그대로 통과해 지나갔다. 그날부터 꼬마의 우비 소매가 찢어져 있었다. 나를 밀어낸 건 꼬마였다.  
   `Cut_S_N5_03` (앵커 KID · 폴백 `Cut_CS5_3`)  
   _coastal road, a truck's headlights passing straight through the translucent figure of 19-year-old HANEUL in a sky-blue hoodie, the tiny child in the orange rain coat pushing her from the side, one sleeve of the coat torn, dramatic_
4. 다쳤냐고 물었다. 꼬마는 소매를 감추며 웃기만 했다. 나 때문이라는 걸, 그때는 몰랐다.  
   `Cut_S_N5_04` (앵커 KID · 폴백 `Cut_V_N5_1`)  
   _the tiny child in the oversized orange rain coat hiding a torn sleeve behind its back and smiling, 19-year-old HANEUL in a sky-blue hoodie kneeling in front asking with worry, wet road after rain_
5. 밤사이 누군가 리어카 바퀴를 새로 끼워 놓았다. 키 큰 사람이었다고, 꼬마가 말했다.  
   `Cut_S_N5_05` (앵커 KID · 폴백 `Cut_CS5_4`)  
   _morning, the old wooden handcart now with a new wheel fitted, the tiny child in the orange rain coat patting the wheel, 19-year-old HANEUL in a sky-blue hoodie looking down the empty road_
6. **— 회상 · 여덟 해 전 —** 폭우의 밤. 마당에 엄마가 엎어져 있었다. 심장이었다. 그 애가 먼저 보고 소리를 질렀다.  
   `Cut_S_N5_06` (앵커 D12 · 폴백 `Cut_V_CH13_Open`)  
   _pouring night rain in a Jeju stone-house yard, HANEUL's mother in plain clothes collapsed face down on the wet ground, 12-year-old DOYUN in a navy vest shouting, 12-year-old HANEUL in a pale-blue t-shirt frozen at the door_
7. **— 회상 · 여덟 해 전 —** 전화는 끊겨 있었고 차는 없었다. 열두 살 둘이 엄마를 리어카에 실었다. 그 애가 자기 자리를 엄마에게 줬다.  
   `Cut_S_N5_07` (앵커 H12 · 폴백 `Cut_V_N5_2`)  
   _heavy night rain, an unconscious grown woman lying on a wooden handcart, two small 12-year-old children HANEUL and DOYUN straining to push the cart together, a dead phone on the wet ground_
8. **— 회상 · 여덟 해 전 —** 병원까지 2킬로. 빗길에서 리어카를 밀었다. 그 애 몸이 먼저 무너졌지만 손은 놓지 않았다.  
   `Cut_S_N5_08` (앵커 D12 · 폴백 `Cut_CS5_6`)  
   _rain-soaked night road, 12-year-old DOYUN in a mud-stained navy vest stumbling to his knees while still gripping the handle of the handcart, 12-year-old HANEUL pulling from the front, town lights far ahead_
9. **— 회상 · 여덟 해 전 —** 엄마는 살았다. 그 애는 병원 복도에서 쓰러졌다. 검은 차가 왔고, 서울로 실려 갔다. 인사도 못 했다.  
   `Cut_S_N5_09` (앵커 D12 · 폴백 `Cut_V_N5_3`)  
   _hospital corridor at night, long white hallway with fluorescent lights, 12-year-old DOYUN in a navy vest slumped asleep on a plastic chair, a tall bodyguard in a black suit walking in through the glass door, 12-year-old HANEUL peeking from a corner_
10. **— 회상 · 여덟 해 전 —** 무서웠어? 응. 나도. 그 밤 우리가 나눈 말은 그게 전부였다. 그 세 마디로 8년을 살았다.  
   `Cut_S_N5_10` (앵커 H12 · 폴백 `Cut_V_CH13_Close`)  
   _two small 12-year-old children HANEUL and DOYUN sitting side by side on hospital steps at dawn wrapped in one blanket, exhausted, rain stopped, first light_
11. 리어카 손잡이에 오래된 흠집이 있었다. 작은 손톱자국. 여덟 해 전 그 밤의 것이었다. 내 손이 꼭 맞았다.  
   `Cut_S_N5_11` (앵커 H19 · 폴백 `Cut_V_CH12_Open`)  
   _close-up of 19-year-old HANEUL's hand gripping the worn wooden handle of the old handcart, old small fingernail scratches on the wood, sky-blue hoodie sleeve, soft light_
12. 꼬마가 리어카 위에 올라앉았다. 「밀어 줘.」 나는 웃으며 밀었다. 바퀴가 처음으로 잘 굴렀다.  
   `Cut_S_N5_12` (앵커 KID · 폴백 `Cut_V_N5_2`)  
   _sunset coastal road, the tiny child in the orange rain coat sitting on the old handcart, 19-year-old HANEUL in a sky-blue hoodie pushing it and laughing, long shadows, tower ahead_

## CS6 「스무 살」 · BGM_M4 · 12컷 × 8.5s · 채도 0.42

1. 정류장에서 탑까지 4.2킬로. 매일 달리던 길인데 오늘은 거기까지 못 갈 것 같다. 발이 자꾸 멈춘다.  
   `Cut_S_N6_01` (앵커 H19 · 폴백 `Cut_V_CH20_Mid`)  
   _19-year-old HANEUL in a sky-blue hoodie standing still in the middle of the coastal road holding her skateboard, wind in her hair and the grass, the steel tower shimmering far away_
2. **— 회상 · 여덟 해 전 —** 떠나던 날, 우리에게 주어진 시간은 5분이었다. 검은 차가 시동을 건 채 기다리고 있었다.  
   `Cut_S_N6_02` (앵커 D12 · 폴백 `Cut_V_Open_7`)  
   _a black sedan idling on the coastal road with the bodyguard holding the rear door, 12-year-old DOYUN in white shirt and navy vest standing beside it facing 12-year-old HANEUL in a pale-blue t-shirt, overcast_
3. **— 회상 · 여덟 해 전 —** 스무 살 네 생일에 송전탑 밑에서 기다릴게. 그 애가 말했다. 8년이나 남았는데. 나는 대답을 못 했다.  
   `Cut_S_N6_03` (앵커 D12 · 폴백 `Cut_V_CH16_Open`)  
   _close-up, 12-year-old DOYUN in a navy vest speaking earnestly to 12-year-old HANEUL, the steel transmission tower on the hill behind them, her eyes wide_
4. **— 회상 · 여덟 해 전 —** 그 애는 우유 두 병을 내밀며 말했다. 하나는 오늘, 하나는 내일 마셔. 난 기다리는 게 특기야.  
   `Cut_S_N6_04` (앵커 D12 · 폴백 `Cut_V_CH14_Close`)  
   _12-year-old DOYUN in white shirt and navy vest holding out two glass milk bottles to 12-year-old HANEUL in a pale-blue t-shirt, close two-shot, soft grey light_
5. **— 회상 · 여덟 해 전 —** 차가 멀어질 때 나는 소리쳤다. 안 늦을게! 그 애 입 모양은 「알아」였다.  
   `Cut_S_N6_05` (앵커 H12 · 폴백 `Cut_V_CH19_Close`)  
   _12-year-old HANEUL in a pale-blue t-shirt running after a black sedan on a coastal road shouting, 12-year-old DOYUN's face in the rear window mouthing a word, the tower behind_
6. **— 회상 · 여덟 해 전 —** 그날 밤 달력에 처음으로 글씨를 썼다. 스무 살 생일, 송전탑 아래. 그 한 줄로 7년을 버텼다.  
   `Cut_S_N6_06` (앵커 H12 · 폴백 `Cut_V_N6_1`)  
   _12-year-old HANEUL in a pale-blue t-shirt at night writing on a wall calendar with a pencil by a small lamp, drawing a tiny heart on one date_
7. 열아홉이 되던 해 엄마는 폐가 나빠 도시 병원으로 갔다. 나는 엄마 해녀복을 입었다. 바다는 내가 지켜야 했다.  
   `Cut_S_N6_07` (앵커 H19 · 폴백 `Cut_V_N6_2`)  
   _19-year-old HANEUL wearing a black haenyeo neoprene wetsuit with the hood down and her long hair out, standing on a rocky shore at dawn holding an orange tewak float with a net bag, her mother's older wetsuit hanging on a rack beside her, resolve_
8. 출근하는 아침마다 벽의 주황 우비가 떨어져 있었다. 올려놓았다. 다음 날 또 떨어져 있었다. 못은 멀쩡했다.  
   `Cut_S_N6_08` (앵커 H19 · 폴백 `Cut_V_N1_1`)  
   _an old orange rain coat lying on the wooden floor below an empty hook on the wall of a Jeju house, 19-year-old HANEUL in a black haenyeo wetsuit bending to pick it up, morning light, the hook clearly intact_
9. 세 번째 아침에도 우비는 바닥에 있었다. 나는 우비를 한 번 안았다가 못에 걸고 나갔다. 그게 마지막이었다.  
   `Cut_S_N6_09` (앵커 H19 · 폴백 `Cut_V_N1_1`)  
   _19-year-old HANEUL in a black haenyeo wetsuit hugging an old orange rain coat to her chest in a dim room before hanging it on the hook, eyes closed, a sliver of morning sea through the door_
10. 열아홉 생일 아침. 아빠 부표를 안고 바다로 들어갔다. 마지막 잠수를 하려고. 하늘이 이상하게 고요했다.  
   `Cut_S_N6_10` (앵커 H19 · 폴백 `Cut_V_N6_3`)  
   _19-year-old HANEUL in a black haenyeo wetsuit wading into a calm dawn sea holding an orange tewak float and a white buoy with two red hearts, the steel tower on the cliff above, eerie stillness_
11. 파도는 조용히 왔다. 그리고 기억은 거기서 끊긴다. 그다음 눈을 뜬 곳이 그 방이었다.  
   `Cut_S_N6_11` (앵커 - · 폴백 `Cut_CS6_7`)  
   _underwater view, a hand in a black wetsuit sleeve letting go of a white heart buoy, rising bubbles, shafts of pale light from the surface, deep blue_
12. 달력의 그 한 줄이 나를 깨웠다. 스무 살 생일까지 1년. 약속 하나가 나를 여기 붙들어 두고 있다.  
   `Cut_S_N6_12` (앵커 H19 · 폴백 `Cut_V_CH20_Open`)  
   _19-year-old HANEUL in a sky-blue hoodie standing in the old room touching the circled date on the wall calendar, the orange rain coat back on its hook beside the door, morning_

## CS7 「둘 중 하나」 · BGM_M1 · 12컷 × 8.5s · 채도 0.32

1. 어느 날부터 기억이 더는 돌아오지 않았다. 대신 탑 아래 낯익은 남자가 서 있었다. 도윤이었다. 스무 살의.  
   `Cut_S_N7_01` (앵커 D20 · 폴백 `Cut_V_END_A1`)  
   _20-year-old DOYUN in a grey hooded coat standing in a yellow rapeseed field under the steel transmission tower at sunset reading a letter, side view, tall and pale_
2. 꼬마가 말했다. 저 사람 안다고. 어떻게 아느냐고는 묻지 않았다. 꼬마는 모르는 게 없었으니까.  
   `Cut_S_N7_02` (앵커 KID · 폴백 `Cut_V_CH17_Open`)  
   _the tiny child in the orange rain coat tugging the sleeve of 19-year-old HANEUL in a sky-blue hoodie and pointing at the distant figure of 20-year-old DOYUN under the tower, dusk_
3. 그는 새벽마다 약을 한 움큼 삼켰다. 손목의 병원 팔찌는 끊긴 적이 없었다. 여전히 아픈 사람이었다.  
   `Cut_S_N7_03` (앵커 D20 · 폴백 `Cut_CS7_3`)  
   _close-up of 20-year-old DOYUN's pale hand with a white hospital bracelet holding a handful of pills, a glass of water, grey dawn light through a window_
4. 탑 아래 우유 두 병을 두고 가는 사람은 그였다. 하나는 오늘, 하나는 내일. 8년째 같은 자리.  
   `Cut_S_N7_04` (앵커 D20 · 폴백 `Cut_V_N7_1`)  
   _20-year-old DOYUN in a grey hooded coat sitting on a rock under the transmission tower at dusk, two glass milk bottles placed on the rock beside him, looking up at the tower_
5. 길에서 넘어졌다. 아무도 보지 못했다. 꼬마만 옆에 앉아 무릎을 털어 줬다.  
   `Cut_S_N7_05` (앵커 KID · 폴백 `Cut_V_CH07_Close`)  
   _19-year-old HANEUL in a sky-blue hoodie fallen on a coastal road with her skateboard rolled away, the tiny child in the orange rain coat kneeling beside her dusting her knee, passers-by walking past without noticing_
6. 정류장 한가운데서 두 팔을 흔들었다. 그는 나를 그대로 통과해 걸어갔다. 안 보는 게 아니라, 볼 수 없는 거였다.  
   `Cut_S_N7_06` (앵커 D20 · 폴백 `Cut_V_CH15_Open`)  
   _stone bus shelter, 19-year-old HANEUL in a sky-blue hoodie waving both arms, 20-year-old DOYUN in a grey hooded coat walking straight through her translucent body, her figure half transparent_
7. 우편함에 또 한 통이 들어왔다. 8년 동안 스무 통. 나는 1년째 한 통도 뜯지 못했다.  
   `Cut_S_N7_07` (앵커 H19 · 폴백 `Cut_V_PRO_A`)  
   _rusty red mailbox at the gate of the Jeju stone house with a new white envelope sticking out, 19-year-old HANEUL in a sky-blue hoodie standing before it not touching it, evening_
8. 그가 답장 없는 편지를 또 썼다. 우체국 앞에서 오래 서 있다가, 결국 우편함에 넣었다.  
   `Cut_S_N7_08` (앵커 D20 · 폴백 `Cut_V_N7_2`)  
   _night street lamp, 20-year-old DOYUN in a grey hooded coat standing before a red post box holding an envelope, hesitating, stars_
9. 그 옆에 앉았다. 같은 노을을 봤다. 그는 우유 한 병을 마시고 한 병은 그대로 두었다. 내 몫이었다.  
   `Cut_S_N7_09` (앵커 D20 · 폴백 `Cut_V_N7_3`)  
   _20-year-old DOYUN in a grey hooded coat and 19-year-old HANEUL in a sky-blue hoodie sitting on a bench facing the sunset sea, he drinks from one milk bottle, the other bottle stands between them untouched, she looks at him, he does not see her_
10. 그의 방 창가에 국화 한 다발이 있었다. 리본엔 이름이 없었다. 누구를 위한 꽃인지 물을 수가 없었다.  
   `Cut_S_N7_10` (앵커 D20 · 폴백 `Cut_V_CH18_Open`)  
   _a bundle of white chrysanthemums with a plain ribbon on a windowsill in a small room, 20-year-old DOYUN's grey coat on a chair, 19-year-old HANEUL's translucent reflection faintly in the window glass_
11. 꼬마는 그를 보면 늘 한 걸음 물러났다. 「저 사람은 아직 몰라.」 무엇을 모른다는 건지 묻지 못했다.  
   `Cut_S_N7_11` (앵커 KID · 폴백 `Cut_V_CH17_Open`)  
   _the tiny child in the orange rain coat stepping back behind 19-year-old HANEUL in a sky-blue hoodie as 20-year-old DOYUN passes on the road at dusk, the child's face serious under the hood_
12. 나란히 창을 봤다. 유리엔 한 사람만 비쳤다. 둘 중 하나는 여기 없다. 그게 누군지, 아직 묻지 못했다.  
   `Cut_S_N7_12` (앵커 D20 · 폴백 `Cut_V_CH18_Open`)  
   _night, 20-year-old DOYUN in a grey hooded coat and 19-year-old HANEUL in a sky-blue hoodie standing side by side before a dark window, only DOYUN's reflection appears in the glass, hers is missing_

## CS8 「주파수」 · BGM_M5 · 12컷 × 8.5s · 채도 0.25

1. 생일 전날은 아빠 기일. 그와 나란히 4.2킬로를 걸었다. 그는 몰랐다. 나도 이제야 알 것 같았다.  
   `Cut_S_N8_01` (앵커 D20 · 폴백 `Cut_V_CH19_Open`)  
   _20-year-old DOYUN in a grey hooded coat and 19-year-old HANEUL in a sky-blue hoodie walking side by side on the coastal road toward the transmission tower, he carries white chrysanthemums, she is slightly translucent, evening_
2. 탑 아래 꽃이 두 다발 놓여 있었다. 아빠 몫 하나. 나머지 하나는 누구 거지. 국화는 내 이름을 알고 있었다.  
   `Cut_S_N8_02` (앵커 H19 · 폴백 `Cut_CS8_2`)  
   _two bundles of white chrysanthemums laid on the grass at the foot of the steel transmission tower beside three stacked stones, a small card tucked in one bundle, 19-year-old HANEUL's sneakers at the edge_
3. 그의 팔찌가 떨어졌고 내 손에 잡혔다. 1년 만에 처음으로 손에 잡힌 물건. 이름이 적혀 있었다. 하늘.  
   `Cut_S_N8_03` (앵커 H19 · 폴백 `Cut_CS8_3`)  
   _close-up of 19-year-old HANEUL's open palm holding a white hospital bracelet with a name written on it, 20-year-old DOYUN's wrist blurred in the background, golden light_
4. 환자 이름: 하늘. 실종 1년. 그는 내 팔찌를 차고 있었다. 나를 찾겠다고 병원에서 뛰쳐나온 사람의 팔찌.  
   `Cut_S_N8_04` (앵커 H19 · 폴백 `Cut_V_N8_1`)  
   _19-year-old HANEUL in a sky-blue hoodie staring at the hospital bracelet in her hands with tears welling, close-up face, the tower and dusk sky behind_
5. 그러니까 나는 이미 죽은 거구나. 아니, 아직 못 찾은 거구나. 달력의 약속 옆에 내 이름을 썼다. 하늘.  
   `Cut_S_N8_05` (앵커 H19 · 폴백 `Cut_V_CH20_Open`)  
   _19-year-old HANEUL in a sky-blue hoodie writing her own name in pencil beside the circled date on the wall calendar, her hand faintly translucent, dim room_
6. 스무 번째 생일. 집엔 내 것이 하나도 없었다. 그릇도, 신발도. 탑 아래 우유 한 병만 내 것이었다.  
   `Cut_S_N8_06` (앵커 H19 · 폴백 `Cut_V_N8_2`)  
   _19-year-old HANEUL in a sky-blue hoodie sitting alone on a bare bed in an emptied Jeju room, no shoes by the door, one glass milk bottle in her hands, morning light_
7. 우유병 하나가 서리 속에서 빛났다. 그는 오늘도 두 병을 놓고 갔다. 8년째, 단 하루도 빠짐없이.  
   `Cut_S_N8_07` (앵커 D20 · 폴백 `Cut_V_END_B1`)  
   _close-up of a glass milk bottle covered in frost glowing in the low winter sun on a rock under the tower, a second bottle beside it, 20-year-old DOYUN's grey coat walking away out of focus_
8. 같은 태풍이 왔다. 같은 정류장. 꼬마는 처음으로 곁에 없었다. 새 부표의 페인트가 손에 묻었다. 아빠였구나.  
   `Cut_S_N8_08` (앵커 H19 · 폴백 `Cut_V_N8_3`)  
   _storm wind and rain at the stone bus shelter, 19-year-old HANEUL in a sky-blue hoodie alone holding a white buoy with a freshly painted red heart, red paint on her fingers, the shelter empty beside her_
9. 탑 아래 돌 세 개 옆에 주황 우비가 개켜져 있었다. 소매가 찢어진 우비. 꼬마는 어디에도 없었다.  
   `Cut_S_N8_09` (앵커 H19 · 폴백 `Cut_V_CH12_Close`)  
   _at the foot of the steel transmission tower, an orange rain coat with a torn sleeve neatly folded beside three stacked stones, rain-wet grass, 19-year-old HANEUL in a sky-blue hoodie kneeling before it_
10. 정류장에서 탑까지 4.2킬로. 이번엔 이기려고 달리는 게 아니다. 닿으려고 달린다.  
   `Cut_S_N8_10` (앵커 H19 · 폴백 `Cut_V_CH20_Mid`)  
   _19-year-old HANEUL in a sky-blue hoodie sprinting on her skateboard along the storm-swept coastal road toward the steel tower, rain streaks, determined face_
11. 마지막 4.2킬로. 꼬마가 없었다. 처음으로 혼자 일어나야 했다. 노을 전에 닿으면, 그가 나를 볼지도 모른다.  
   `Cut_S_N8_11` (앵커 H19 · 폴백 `Cut_V_CH01_Close`)  
   _19-year-old HANEUL in a sky-blue hoodie pushing herself up from the wet road alone, skateboard beside her, the sunset breaking through storm clouds behind the transmission tower ahead_
12. 탑 아래 그가 서 있었다. 국화 두 다발을 들고. 나는 숨을 고르고 그의 이름을 불렀다. 도윤아.  
   `Cut_S_N8_12` (앵커 D20 · 폴백 `Cut_V_END_A1`)  
   _sunset breaking through the clouds, 20-year-old DOYUN in a grey hooded coat standing under the steel transmission tower holding two bundles of white chrysanthemums, 19-year-old HANEUL in a sky-blue hoodie arriving out of breath in the foreground seen from behind_

## END_A 「스무 살 생일 · 만난다」 · BGM_M3 · 8컷 × 8.5s · 채도 0.6

1. 노을 반. 탑 아래에 엄마가 무릎을 꿇고 국화를 놓았다. 어제 그가 놓은 두 다발 옆에.  
   `Cut_S_EA_01` (앵커 D20 · 폴백 `Cut_V_CH19_Open`)  
   _sunset under the steel transmission tower, HANEUL's mother in plain dark clothes kneeling to lay white chrysanthemums beside two other bundles, 20-year-old DOYUN in a grey hooded coat sitting a few steps away with a hand on his chest_
2. 엄마가 일어서다 그를 봤다. 8년 전 빗속에서 자기를 리어카에 싣고 간 아이. 「많이 컸수다.」  
   `Cut_S_EA_02` (앵커 D20 · 폴백 `Cut_V_END_A1`)  
   _HANEUL's mother standing face to face with 20-year-old DOYUN under the tower at sunset, she looks up at him with tears, he bows his head, 19-year-old HANEUL in a sky-blue hoodie standing translucent between them unseen_
3. 엄마. 나 여기 있어. 두 사람 사이에서 손을 들었다. 둘 다 보지 않았다. 엄마가 언덕을 내려갔다.  
   `Cut_S_EA_03` (앵커 H19 · 폴백 `Cut_V_CH15_Open`)  
   _19-year-old HANEUL in a sky-blue hoodie raising her hand between her mother and DOYUN, both looking past her, the mother walking down the hill, the sunset shrinking_
4. 도윤아. 그가 돌아봤다. 얼굴이 환해졌다. 내 얼굴부터 색이 돌았다. 그의 얼굴, 억새, 노을, 바다.  
   `Cut_S_EA_04` (앵커 D20 · 폴백 `Cut_V_MV_6`)  
   _20-year-old DOYUN in a grey hooded coat turning around with a bright astonished smile, 19-year-old HANEUL in a sky-blue hoodie fully solid and glowing with color, silver grass and sunset sea blazing with color around them_
5. 무사 이추룩 늦언. 너 그거 어디서 배웠어. 8년. 나는 스무 챕터 만에 처음으로 소리 내어 웃었다.  
   `Cut_S_EA_05` (앵커 H19 · 폴백 `Cut_V_MV_3`)  
   _close two-shot, 19-year-old HANEUL in a sky-blue hoodie laughing out loud with tears, 20-year-old DOYUN in a grey hooded coat laughing back, sunset light on their faces, the tower behind_
6. 꼬마가 돌 세 개 위에 하나를 더 얹었다. 네 개. 왜 더 쌓아. 또 나가야 되니까. 언제 와. 내년에.  
   `Cut_S_EA_06` (앵커 KID · 폴백 `Cut_V_CH12_Close`)  
   _the tiny child in the orange rain coat placing a fourth stone on the cairn under the tower at dusk, 19-year-old HANEUL in a sky-blue hoodie and 20-year-old DOYUN watching from behind, silver grass closing_
7. 봄. 유채가 진짜 노란 해안도로. 그가 걷고, 옆에서 보드 바퀴 소리가 났다. 그가 옆을 보며 웃었다.  
   `Cut_S_EA_07` (앵커 D20 · 폴백 `Cut_V_MV_1`)  
   _spring coastal road blazing with yellow rapeseed, 20-year-old DOYUN in a grey hooded coat walking and smiling sideways at 19-year-old HANEUL in a sky-blue hoodie rolling beside him on a pink skateboard, bright sky_
8. 정류장. 운행 재개. 버스 문이 열리자 그가 반걸음 물러나 손을 내밀었다. 먼저 타. 우유 두 개. 딸깍, 딸깍.  
   `Cut_S_EA_08` (앵커 D20 · 폴백 `Cut_V_N1_2`)  
   _a country bus stopped at the stone bus shelter with its door open, 20-year-old DOYUN in a grey hooded coat stepping back and holding out his hand toward the door, two glass milk bottles in his other hand, 19-year-old HANEUL in a sky-blue hoodie stepping up, warm morning_

## END_B 「우유 두 병 · 못 만난다」 · BGM_M6 · 8컷 × 8.5s · 채도 0.35

1. 노을 반. 탑 아래에 엄마가 국화를 놓았다. 조금 떨어진 곳에 그가 앉아 있었다. 한 손을 가슴에 얹고.  
   `Cut_S_EB_01` (앵커 D20 · 폴백 `Cut_V_CH19_Open`)  
   _sunset under the steel transmission tower, HANEUL's mother in plain dark clothes laying white chrysanthemums beside two other bundles, 20-year-old DOYUN in a grey hooded coat sitting on a rock apart with one hand pressed to his chest_
2. 도윤아. 그가 천천히 일어나 돌아봤다. 눈이 반 뼘 어긋나 있었다. 거기 있어? 있는 것 같은데.  
   `Cut_S_EB_02` (앵커 D20 · 폴백 `Cut_V_CH15_Open`)  
   _20-year-old DOYUN in a grey hooded coat standing and looking slightly past 19-year-old HANEUL in a sky-blue hoodie, his eyes not quite meeting hers, she is half transparent, dusk_
3. 허공을 더듬는 손. 나는 그 손을 잡았다. 두 손 사이에서 하트 하나가 켜졌다. 화면에서 유일한 색.  
   `Cut_S_EB_03` (앵커 D20 · 폴백 `Cut_V_MV_3`)  
   _close-up of two hands clasped in grey dusk, 20-year-old DOYUN's hand and 19-year-old HANEUL's translucent hand, a small glowing red heart of light between the palms, everything else desaturated_
4. 나 보여? 아니. 근데 잡혀. 나는 손목의 팔찌를 빼서 그의 손목에 끼웠다. 그가 손목을 만졌다.  
   `Cut_S_EB_04` (앵커 H19 · 폴백 `Cut_CS8_3`)  
   _19-year-old HANEUL in a sky-blue hoodie fastening a white hospital bracelet onto the wrist of 20-year-old DOYUN who stares at empty air, his other hand touching the bracelet, sunset fading_
5. 국화 뒤에 세워진 액자 하나. 검은 리본. 내 얼굴이었다. 마지막 조각 — 물속, 부표를 쥔 손, 멈추는 기포.  
   `Cut_S_EB_05` (앵커 - · 폴백 `Cut_CS6_7`)  
   _underwater, a hand in a black wetsuit sleeve gripping a white buoy with two red hearts, the bubbles stopping, pale light from the surface, deep blue, very still_
6. 바다야! 억새에서 주황 우비가 올라왔다. 너 나 몇 번 일으켰어. 몰라. 나도 몰라. 많이.  
   `Cut_S_EB_06` (앵커 KID · 폴백 `Cut_V_CH17_Open`)  
   _the tiny child in the orange rain coat standing in tall silver grass under the tower at dusk looking up at 19-year-old HANEUL in a sky-blue hoodie, 20-year-old DOYUN in the background looking at empty air_
7. 돌 세 개를 손으로 쓸어 무너뜨렸다. 돌아왔으니까. 주머니에서 하트 돌 하나. 아빠 맞지. 한 마리만 더 잡으민 되주.  
   `Cut_S_EB_07` (앵커 KID · 폴백 `Cut_V_N4_1`)  
   _close-up of a small hand in an orange rain coat sleeve placing a heart-shaped pebble into 19-year-old HANEUL's open palm, three stones scattered on the grass, the child's face hidden by the hood, dusk_
8. 다음 날 아침. 가게. 오늘도 두 개라? 예. 두 개마씸. 문이 닫히고 종이 한 번 더 울렸다. 아무도 안 나갔는데.  
   `Cut_S_EB_08` (앵커 D20 · 폴백 `Cut_V_N7_2`)  
   _morning inside a small Jeju corner shop, 20-year-old DOYUN in a grey hooded coat holding two glass milk bottles at the counter, an old shopkeeper smiling, the door with a bell, warm light_

## END_TRUE 「주파수 · 진엔딩」 · BGM_M1 · 8컷 × 8.5s · 채도 0.8

1. 이듬해 봄. 새벽 두 시. 91.9. 오늘은 사연이 아니라 목소리로 왔습니다. 제주에서, 열아홉 살…  
   `Cut_S_ET_01` (앵커 H19 · 폴백 `Cut_V_N3_3`)  
   _night, 19-year-old HANEUL in a sky-blue hoodie sitting on the floor of the old Jeju room beside a small glowing radio, the dial lit, window dark, tower lamp outside_
2. 잡음. 그리고 아는 목소리. 하늘아. 규칙이 하나 더 있었어. 네가 두 번 다 끝까지 오면, 한 번 더 말할 수 있대.  
   `Cut_S_ET_02` (앵커 H19 · 폴백 `Cut_V_N3_3`)  
   _close-up of an old radio dial glowing 91.9 with soft static light, 19-year-old HANEUL's hand resting on it, warm amber glow in a dark room_
3. 늦게 와도 된다고 했잖아. 취소. 늦게 오지 마. 내년 봄에도 그 자리에 있을 거야. 노을 질 때.  
   `Cut_S_ET_03` (앵커 D20 · 폴백 `Cut_V_N7_2`)  
   _20-year-old DOYUN in a grey hooded coat speaking into a microphone in a small radio studio at night, a hospital bracelet on his wrist, soft booth light_
4. 창밖 송전탑에 불이 하나 켜져 있었다. 응. 안 늦어.  
   `Cut_S_ET_04` (앵커 H19 · 폴백 `Cut_V_CH18_Open`)  
   _19-year-old HANEUL in a sky-blue hoodie at the window at night looking at the steel transmission tower with a single red lamp lit against the stars, a small smile_
5. 그리고 유채가 하루 일찍 피었다.  
   `Cut_S_ET_05` (앵커 - · 폴백 `Cut_V_OP_1`)  
   _dawn over the Jeju hill, the rapeseed field under the steel transmission tower bursting into yellow bloom, first sunlight, no people_
6. 노을 질 때, 탑 아래. 두 사람이 마주 보고 서 있었다. 이번엔 둘 다 보였다.  
   `Cut_S_ET_06` (앵커 D20 · 폴백 `Cut_V_MV_6`)  
   _sunset under the steel transmission tower, 19-year-old HANEUL in a sky-blue hoodie and 20-year-old DOYUN in a grey hooded coat standing face to face, both fully solid and colorful, white buoys with red hearts on the rocks below_
7. 우리의 주파수. 91.9.  
   `Cut_S_ET_07` (앵커 - · 폴백 `Cut_V_MV_4`)  
   _wide shot, the couple 19-year-old HANEUL and 20-year-old DOYUN as small silhouettes sitting together at the foot of the tower against a huge sunset sky, an old radio between them, sea below_
8. — 너와 나의 주파수 —  
   `Cut_S_ET_08` (앵커 - · 폴백 `Cut_V_MV_5`)  
   _title card visual: the steel transmission tower on the Jeju cliff at golden hour with rapeseed in bloom and a calm sea, warm light rays, no people_