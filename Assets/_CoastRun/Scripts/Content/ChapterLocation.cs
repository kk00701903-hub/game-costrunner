using UnityEngine;

namespace CoastRun
{
    /// 챕터 20 = 제주 로케이션 20 (제주특화_재설계_v1 §4). 런 트랙의 원경·세트·포토스팟 이름을 정한다.
    /// 그림: Resources/CoastRun/Far_<Set>_<SEASON> → Far_<Set> → Far_Town_<SEASON> → Far_Town_NOON 순으로 찾는다.
    public enum TrackSet { Coast, Village, Oreum, Forest, Cave }

    public struct LocationDef
    {
        public string ko, en, spotKo, spotEn;
        public TrackSet set;
        public LocationDef(string ko, string en, TrackSet set, string spotKo, string spotEn)
        { this.ko = ko; this.en = en; this.set = set; this.spotKo = spotKo; this.spotEn = spotEn; }
        public string Name => Loc.T(ko, en);
        public string Spot => Loc.T(spotKo, spotEn);
    }

    public static class ChapterLocation
    {
        private static readonly LocationDef[] Table =
        {
            new LocationDef("애월 무지개 해안도로", "Aewol Rainbow Coastal Road", TrackSet.Coast, "무지개 방호벽", "Rainbow barrier"),
            new LocationDef("이호테우 해변", "Iho Tewoo Beach", TrackSet.Coast, "목마등대", "Horse lighthouse"),
            new LocationDef("하늘네 마을", "Haneul's Village", TrackSet.Village, "정낭 대문", "Jeongnang gate"),
            new LocationDef("칠머리당 영등굿 마을", "Chilmeoridang Yeongdeung Village", TrackSet.Village, "오색 천", "Five-color cloth"),
            new LocationDef("섭지코지 유채 초원", "Seopjikoji Canola Fields", TrackSet.Oreum, "유채꽃밭", "Canola field"),
            new LocationDef("이호테우 밤 축제", "Iho Tewoo Night Festival", TrackSet.Coast, "등불", "Lanterns"),
            new LocationDef("성산 해녀 탈의장", "Seongsan Haenyeo Shore", TrackSet.Coast, "성산일출봉", "Seongsan Ilchulbong"),
            new LocationDef("제주시 동문시장", "Jeju City Dongmun Market", TrackSet.Village, "시장 골목", "Market alley"),
            new LocationDef("우도 원담", "Udo Wondam Stone Weir", TrackSet.Coast, "원담", "Wondam"),
            new LocationDef("곶자왈 숲길", "Gotjawal Forest", TrackSet.Forest, "이끼 바위", "Moss rock"),
            new LocationDef("성읍민속마을 귤밭", "Seongeup Folk Village", TrackSet.Village, "귤밭", "Tangerine grove"),
            new LocationDef("다랑쉬오름 능선", "Darangshi Oreum Ridge", TrackSet.Oreum, "능선", "Ridge"),
            new LocationDef("중산간 메밀밭", "Mid-mountain Buckwheat Fields", TrackSet.Oreum, "메밀꽃", "Buckwheat blossom"),
            new LocationDef("만장굴", "Manjanggul Lava Tube", TrackSet.Cave, "동굴 입구", "Cave mouth"),
            new LocationDef("송악산 억새 절벽", "Songaksan Silver-grass Cliff", TrackSet.Oreum, "억새", "Silver grass"),
            new LocationDef("본향당 첫눈 마을", "Bonhyangdang Shrine, First Snow", TrackSet.Village, "신당", "Shrine"),
            new LocationDef("오름 입구 방사탑", "Bangsatap Stone Tower", TrackSet.Oreum, "방사탑", "Bangsatap"),
            new LocationDef("동백 숲", "Camellia Forest", TrackSet.Forest, "동백", "Camellia"),
            new LocationDef("산방산 전망 해안", "Sanbangsan Coast", TrackSet.Coast, "산방산", "Sanbangsan"),
            new LocationDef("오름 정상 · 송전탑", "Oreum Summit · The Tower", TrackSet.Oreum, "송전탑", "The tower"),
        };

        public static LocationDef Get(int stage) => Table[Mathf.Clamp(stage, 1, Table.Length) - 1];
        public static LocationDef Current => Get(ChapterDifficulty.Stage);

        public static string SetKey(TrackSet s)
        {
            switch (s)
            {
                case TrackSet.Village: return "Village";
                case TrackSet.Oreum: return "Oreum";
                case TrackSet.Forest: return "Forest";
                case TrackSet.Cave: return "Cave";
                default: return "Coast";
            }
        }
    }
}
