using DsmrHub.Infrastructure.Dsmr;
using Xunit;

namespace DsmrHub.Tests;

public class TelegramFramerTests
{
    private static string NextTelegram() => new SyntheticTelegramGenerator().Next();

    private static void AssertParses(string telegram)
        => Assert.True(new DsmrTelegramParser().TryParse(telegram, out _));

    [Fact]
    public void Whole_telegram_in_one_chunk_is_released()
    {
        var telegram = NextTelegram();

        var result = new TelegramFramer().Append(telegram);

        AssertParses(Assert.Single(result));
    }

    [Fact]
    public void Telegram_split_across_chunks_is_released_once_complete()
    {
        // The regression: a telegram arriving in two serial reads was parsed as two fragments,
        // both failed, and the reading was silently dropped.
        var telegram = NextTelegram();
        var half = telegram.Length / 2;
        var framer = new TelegramFramer();

        Assert.Empty(framer.Append(telegram[..half]));
        var result = framer.Append(telegram[half..]);

        AssertParses(Assert.Single(result));
    }

    [Fact]
    public void Telegram_delivered_one_character_at_a_time_is_released_exactly_once()
    {
        var telegram = NextTelegram();
        var framer = new TelegramFramer();
        var released = new List<string>();

        foreach (var c in telegram)
        {
            released.AddRange(framer.Append(c.ToString()));
        }

        AssertParses(Assert.Single(released));
    }

    [Fact]
    public void Split_inside_the_crc_waits_for_the_remaining_characters()
    {
        var telegram = NextTelegram();
        var afterBang = telegram.IndexOf('!') + 2;
        var framer = new TelegramFramer();

        Assert.Empty(framer.Append(telegram[..afterBang]));
        var result = framer.Append(telegram[afterBang..]);

        AssertParses(Assert.Single(result));
    }

    [Fact]
    public void Two_telegrams_in_one_chunk_are_both_released()
    {
        var generator = new SyntheticTelegramGenerator();

        var result = new TelegramFramer().Append(generator.Next() + generator.Next());

        Assert.Equal(2, result.Count);
        Assert.All(result, AssertParses);
    }

    [Fact]
    public void Tail_of_a_telegram_joined_halfway_is_discarded()
    {
        var telegram = NextTelegram();
        var framer = new TelegramFramer();

        // Connecting mid-telegram: the first thing seen is the back half of one.
        Assert.Empty(framer.Append(telegram[(telegram.Length / 2)..]));
        var result = framer.Append(telegram);

        AssertParses(Assert.Single(result));
    }

    [Fact]
    public void Reset_discards_a_partial_telegram()
    {
        var telegram = NextTelegram();
        var half = telegram.Length / 2;
        var framer = new TelegramFramer();

        framer.Append(telegram[..half]);
        framer.Reset();

        Assert.Empty(framer.Append(telegram[half..]));
    }
}
