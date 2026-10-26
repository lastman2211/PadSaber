using System;
using System.Runtime.InteropServices;

namespace NonVRMod.Input
{
    /// <summary>Gamepad via XInput directly, so it works without the game's own input mappings.</summary>
    internal static class XInput
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Gamepad
        {
            public ushort Buttons;
            public byte LeftTrigger, RightTrigger;
            public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct State
        {
            public uint PacketNumber;
            public Gamepad Pad;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint GetState(uint index, out State state);

        public const ushort A = 0x1000, B = 0x2000, Start = 0x0010, Back = 0x0020;
        private const float Dead = 0.18f;

        public static bool Connected { get; private set; }
        public static float LX, LY, RX, RY, LT, RT;
        public static ushort Buttons, PrevButtons;

        public static void Poll()
        {
            PrevButtons = Buttons;
            try
            {
                Connected = GetState(0, out var s) == 0;
                if (!Connected) { LX = LY = RX = RY = LT = RT = 0; Buttons = 0; return; }
                LX = Norm(s.Pad.ThumbLX); LY = Norm(s.Pad.ThumbLY);
                RX = Norm(s.Pad.ThumbRX); RY = Norm(s.Pad.ThumbRY);
                LT = s.Pad.LeftTrigger / 255f; RT = s.Pad.RightTrigger / 255f;
                Buttons = s.Pad.Buttons;
            }
            catch (DllNotFoundException) { Connected = false; }
            catch (EntryPointNotFoundException) { Connected = false; }
        }

        public static bool Down(ushort b) => (Buttons & b) != 0 && (PrevButtons & b) == 0;
        public static bool Held(ushort b) => (Buttons & b) != 0;

        private static float Norm(short v)
        {
            float f = v / 32767f;
            return Math.Abs(f) < Dead ? 0f : f;
        }
    }
}
