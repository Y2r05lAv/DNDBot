using System.Security.Cryptography;

namespace DNDBot.BotMath
{
    public class MathfDND
    {
        public static int DiceReturn(int diceSideCount)
        {
            return RandomNumberGenerator.GetInt32(1, diceSideCount + 1);
        }

    }
}
