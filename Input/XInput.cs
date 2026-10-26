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

        [StructLayout(LayoutKind.Sequential)]
        private struct Vibration
        {
            public ushort Left, Right;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint GetState(uint index, out State state);

        [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
        private static extern uint SetState(uint index, ref Vibration vibration);

        public const ushort DUp = 0x0001, DDown = 0x0002, DLeft = 0x0004, DRight = 0x0008,
                            Start = 0x0010, Back = 0x0020, L3 = 0x0040, R3 = 0x0080,
                            LB = 0x0100, RB = 0x0200, A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000;

        public static float Deadzone = 0.18f;

        public static bool Connected { get; private set; }
        public static float LX, LY, RX, RY, LT, RT;
        public static ushort Buttons, PrevButtons;

        // Rumble: remaining seconds + strength per motor (0 = left/heavy motor, 1 = right/light motor).
        private static readonly float[] _rumbleTime = new float[2];
        private static readonly float[] _rumbleStrength = new float[2];
        private static ushort _sentLeft, _sentRight;

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

        /// <summary>Starts a rumble pulse. motor 0 = left hand, 1 = right hand.</summary>
        public static void Rumble(int motor, float seconds, float strength)
        {
            if (motor < 0 || motor > 1) return;
            _rumbleTime[motor] = Math.Max(_rumbleTime[motor], seconds);
            _rumbleStrength[motor] = Math.Max(_rumbleStrength[motor], Math.Min(1f, strength));
        }

        public static void StopRumble(int motor)
        {
            if (motor < 0 || motor > 1) return;
            _rumbleTime[motor] = 0f;
            _rumbleStrength[motor] = 0f;
        }

        /// <summary>Counts down rumble timers and sends the motor speeds (only when they change).</summary>
        public static void TickRumble(float dt, float master)
        {
            for (int i = 0; i < 2; i++)
            {
                _rumbleTime[i] -= dt;
                if (_rumbleTime[i] <= 0f) { _rumbleTime[i] = 0f; _rumbleStrength[i] = 0f; }
            }
            ushort l = (ushort)(Math.Min(1f, _rumbleStrength[0] * master) * 65535f);
            ushort r = (ushort)(Math.Min(1f, _rumbleStrength[1] * master) * 65535f);
            if (l == _sentLeft && r == _sentRight) return;
            _sentLeft = l; _sentRight = r;
            try
            {
                var v = new Vibration { Left = l, Right = r };
                SetState(0, ref v);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        private static float Norm(short v)
        {
            float f = v / 32767f;
            return Math.Abs(f) < Deadzone ? 0f : f;
        }
    }
}
