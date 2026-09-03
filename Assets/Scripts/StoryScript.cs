using System.Collections.Generic;

/// The written half of the story engine. Kept in code rather than data because
/// these lines are the ones the systems were built around; events.json is for
/// anything added later without a recompile.
public static class StoryScript
{
    /// Names read off retrieval tags. Each one is a person who did not make it
    /// to the depot, and none of them are ever explained.
    private static readonly string[] TagNames =
    {
        "A-0341 김세연",
        "A-0339 박도윤",
        "A-0344 최은비",
        "A-0322 정하람",
        "A-0350 윤시우",
        "A-0318 강예린"
    };

    public static List<StoryEvent> BuiltIn()
    {
        var list = new List<StoryEvent>();

        Opening(list);
        Zones(list);
        RunCountPool(list);
        Pickups(list);
        Damage(list);
        Boss(list);
        Deaths(list);

        return list;
    }

    private static void Opening(List<StoryEvent> list)
    {
        list.Add(Ev("open_retrieval", StoryTrigger.OnDistance, StoryChannel.Radio, Speaker.Retrieval,
            L("자산 A-0347. 회수 절차를 시작합니다. 협조에 감사드립니다.", 3.4f))
            .At(40f));

        list.Add(Ev("open_doha", StoryTrigger.OnDistance, StoryChannel.AmbientVoice, Speaker.Doha,
            L("배달 두 개 남았는데.", 2.4f))
            .At(120f));

        list.Add(Ev("open_bungeo", StoryTrigger.OnDistance, StoryChannel.Radio, Speaker.Bungeo,
            L("야 도하! 야! 무전 켜져 있지? 켜져 있으면 아무 말이나 해!", 3.2f),
            L("…아니 대답 안 해도 돼. 계속 달리기만 해.", 2.8f))
            .At(260f));

        // The list reading, then eight seconds of nothing. The silence is the
        // line; anything under it would be an explanation.
        list.Add(Ev("ihan_list", StoryTrigger.OnDistance, StoryChannel.Cutscene, Speaker.Retrieval,
            L("A-0348. 서이한. 집하 완료.", 2.6f),
            Silent("", 8f))
            .At(900f)
            .Sets("knows_brother"));
    }

    private static void Zones(List<StoryEvent> list)
    {
        list.Add(Ev("zone1", StoryTrigger.OnZoneEnter, StoryChannel.AmbientVoice, Speaker.Doha,
            L("간판 아직 켜져 있네. 전기 어디서 오는 거야.", 3f)).Zone(1));

        list.Add(Ev("zone2", StoryTrigger.OnZoneEnter, StoryChannel.AmbientVoice, Speaker.Doha,
            L("여기 아래 강 있었는데. 지금은 아래가 없네.", 3.2f)).Zone(2));

        list.Add(Ev("zone3", StoryTrigger.OnZoneEnter, StoryChannel.AmbientVoice, Speaker.Doha,
            L("물에 마네킹 떠 있어. 웃고 있고.", 3f)).Zone(3));

        list.Add(Ev("zone4", StoryTrigger.OnZoneEnter, StoryChannel.AmbientVoice, Speaker.Doha,
            L("빨래 아직 걸려 있어. 다 마른 거.", 3f)).Zone(4));

        list.Add(Ev("zone5", StoryTrigger.OnZoneEnter, StoryChannel.AmbientVoice, Speaker.Doha,
            L("여기까지 오면 뭐 있을 줄 알았는데.", 3f)).Zone(5));

        list.Add(Ev("zone3_bungeo", StoryTrigger.OnZoneEnter, StoryChannel.Radio, Speaker.Bungeo,
            L("지하상가! 물 차 있다고 내가 말했지! …아 맞다 너 지금 거기지.", 3.4f)).Zone(3));
    }

    /// The cleaner is the only one who notices how many times this has happened.
    private static void RunCountPool(List<StoryEvent> list)
    {
        list.Add(Ev("sweeper_new", StoryTrigger.OnDistance, StoryChannel.Radio, Speaker.Sweeper,
            L("처음이지? 뒤 돌아보지 마. 그거 한 번 하면 습관 돼.", 3.4f))
            .At(520f).Runs(0, 9));

        list.Add(Ev("sweeper_again", StoryTrigger.OnDistance, StoryChannel.Radio, Speaker.Sweeper,
            L("또 너야. 이 시간에 이 채널 쓰는 애는 너밖에 없어.", 3.4f))
            .At(520f).Runs(10, 49));

        list.Add(Ev("sweeper_deja", StoryTrigger.OnDistance, StoryChannel.Radio, Speaker.Sweeper,
            L("…너 전에도 여기서 나한테 그거 물어봤던 것 같은데.", 3.6f))
            .At(520f).Runs(50, 149));

        list.Add(Ev("sweeper_worn", StoryTrigger.OnDistance, StoryChannel.Radio, Speaker.Sweeper,
            L("이름이 뭐였지. …아니다. 묻지 않는 게 낫겠다.", 3.6f))
            .At(520f).Runs(150, 346));

        // The 347th attempt is the whole title. It does not get a death screen.
        list.Add(Ev("run_347", StoryTrigger.OnRunCountChanged, StoryChannel.Cutscene, Speaker.Retrieval,
            L("자산 A-0347. 회수 시도 347회.", 3f),
            L("회수 불가 판정. 목록에서 삭제합니다.", 3.4f),
            Silent("", 5f))
            .Runs(347, 0).Permanent().Sets("saw_king_face"));
    }

    private static void Pickups(List<StoryEvent> list)
    {
        for (int i = 0; i < TagNames.Length; i++)
        {
            list.Add(Ev("tag_" + i, StoryTrigger.OnItemPickup, StoryChannel.UiLog, Speaker.Retrieval,
                L(TagNames[i] + ". 집하 완료.", 2.2f))
                .Pickup("Tag").Repeatable(1.5f));
        }

        list.Add(Ev("coin_pick", StoryTrigger.OnItemPickup, StoryChannel.AmbientVoice, Speaker.Doha,
            L("이거 이제 아무 데서도 안 받아. 그래도 줍는 게 습관이라.", 3f))
            .Pickup("Coin").Repeatable(90f));

        list.Add(Ev("letter_pick", StoryTrigger.OnItemPickup, StoryChannel.Radio, Speaker.Bungeo,
            L("…그거 찾았어? 뜯어보지 마. 아직.", 2.8f))
            .Pickup("Letter").Repeatable(4f));

        list.Add(Ev("deck_pick", StoryTrigger.OnItemPickup, StoryChannel.AmbientVoice, Speaker.Doha,
            L("누구 데크였을까. 잘 깎았네.", 2.6f))
            .Pickup("DeckPiece").Repeatable(45f));
    }

    private static void Damage(List<StoryEvent> list)
    {
        list.Add(Ev("hurt_1", StoryTrigger.OnDamage, StoryChannel.AmbientVoice, Speaker.Doha,
            L("괜찮아. 데크는 괜찮아.", 2f)).Repeatable(12f));

        list.Add(Ev("hurt_2", StoryTrigger.OnDamage, StoryChannel.AmbientVoice, Speaker.Doha,
            L("…금 갔다. 괜찮아. 아직 굴러가.", 2.2f)).Repeatable(12f));

        list.Add(Ev("hurt_retrieval", StoryTrigger.OnDamage, StoryChannel.Radio, Speaker.Retrieval,
            L("A-0347. 보드에서 내려 주시면 절차가 간단해집니다.", 3f)).Repeatable(28f));
    }

    private static void Boss(List<StoryEvent> list)
    {
        list.Add(Ev("boss_p1", StoryTrigger.OnBossPhase, StoryChannel.Radio, Speaker.Retrieval,
            L("A-0001. 서이한. 회수 담당.", 2.8f)).Phase(1));

        list.Add(Ev("boss_p2", StoryTrigger.OnBossPhase, StoryChannel.AmbientVoice, Speaker.Doha,
            L("형.", 1.6f),
            L("…형 맞잖아. 뒤로 타는 사람 형밖에 없었어.", 3.2f)).Phase(2));

        list.Add(Ev("boss_p3", StoryTrigger.OnBossPhase, StoryChannel.Cutscene, Speaker.Ihan,
            L("내려.", 1.4f),
            Silent("", 3f)).Phase(3).Sets("saw_king_face"));
    }

    private static void Deaths(List<StoryEvent> list)
    {
        list.Add(Ev("death_log", StoryTrigger.OnDeath, StoryChannel.UiLog, Speaker.Retrieval,
            L("A-0347 회수. 기록 보관.", 2.6f)).Repeatable(0f));
    }

    private static StoryLine L(string text, float seconds)
    {
        return new StoryLine { text = text, seconds = seconds };
    }

    private static StoryLine Silent(string text, float seconds)
    {
        return new StoryLine { text = text, seconds = seconds, silence = true };
    }

    private static StoryEvent Ev(
        string id,
        StoryTrigger trigger,
        StoryChannel channel,
        Speaker speaker,
        params StoryLine[] lines)
    {
        return new StoryEvent
        {
            id = id,
            trigger = trigger.ToString(),
            channel = channel.ToString(),
            speaker = speaker.ToString(),
            lines = lines
        };
    }

    private static StoryEvent At(this StoryEvent evt, float metres)
    {
        evt.distance = metres;
        return evt;
    }

    private static StoryEvent Zone(this StoryEvent evt, int zone)
    {
        evt.zone = zone;
        return evt;
    }

    private static StoryEvent Phase(this StoryEvent evt, int phase)
    {
        evt.bossPhase = phase;
        return evt;
    }

    private static StoryEvent Pickup(this StoryEvent evt, string kind)
    {
        evt.pickup = kind;
        return evt;
    }

    private static StoryEvent Runs(this StoryEvent evt, int min, int max)
    {
        evt.minRunCount = min;
        evt.maxRunCount = max;
        return evt;
    }

    private static StoryEvent Repeatable(this StoryEvent evt, float cooldown)
    {
        evt.once = false;
        evt.cooldown = cooldown;
        return evt;
    }

    private static StoryEvent Permanent(this StoryEvent evt)
    {
        evt.permanent = true;
        return evt;
    }

    private static StoryEvent Sets(this StoryEvent evt, params string[] flags)
    {
        evt.setFlags = flags;
        return evt;
    }
}
