using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MouseToFinger
{
    class MouseToFinger
    {
        public MouseToFinger()
        {
            InitializeTouchInjection(1, TOUCH_FEEDBACK_NONE);

            AppDomain.CurrentDomain.DomainUnload += delegate
            {
                Stop();
            };
        }

        public void Start()
        {
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc = MouseHookProc, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
        }

        public void Stop()
        {
            _leftButtonDown = false;
            _mouseMoved = false;

            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
        }

        #region Touch

        private readonly POINTER_TOUCH_INFO[] contacts = new POINTER_TOUCH_INFO[1];

        public void Down(int x, int y)
        {
            contacts[0].pointerInfo.pointerType = POINTER_INPUT_TYPE.PT_TOUCH;
            contacts[0].touchFlags = TouchFlags.NONE;

            contacts[0].orientation = 90;
            contacts[0].pressure = 32000;
            contacts[0].pointerInfo.pointerFlags = POINTER_FLAG.DOWN | POINTER_FLAG.INRANGE | POINTER_FLAG.INCONTACT;
            contacts[0].touchMask = TouchMask.CONTACTAREA | TouchMask.ORIENTATION | TouchMask.PRESSURE;
            contacts[0].pointerInfo.ptPixelLocation.x = x;
            contacts[0].pointerInfo.ptPixelLocation.y = y;
            contacts[0].pointerInfo.pointerId = 0;

            const int marge = 2;
            contacts[0].rcContact.left = x - marge;
            contacts[0].rcContact.right = x + marge;
            contacts[0].rcContact.top = y - marge;
            contacts[0].rcContact.bottom = y - marge;

            InjectTouchInput(1, contacts);
        }

        public void Hold()
        {
            Thread holdThread = new Thread(() => {
                do
                {
                    Thread.Sleep(100);
                    if (_leftButtonDown && !_mouseMoved)
                    {
                        contacts[0].pointerInfo.pointerFlags = POINTER_FLAG.UPDATE | POINTER_FLAG.INRANGE | POINTER_FLAG.INCONTACT;
                        InjectTouchInput(1, contacts);
                    }

                    _mouseMoved = false;

                } while (_leftButtonDown);

            });

            holdThread.Start();
        }

        public void Drag(int x, int y)
        {

            contacts[0].pointerInfo.pointerFlags = POINTER_FLAG.UPDATE | POINTER_FLAG.INRANGE | POINTER_FLAG.INCONTACT;

            contacts[0].pointerInfo.ptPixelLocation.x = x;
            contacts[0].pointerInfo.ptPixelLocation.y = y;

            InjectTouchInput(1, contacts);
        }

        public void Up()
        {
            contacts[0].pointerInfo.pointerFlags = POINTER_FLAG.UP;
            InjectTouchInput(1, contacts);
        }

        [DllImport("User32.dll")]
        private static extern bool InitializeTouchInjection(uint maxCount, int dwMode);

        [DllImport("User32.dll")]
        private static extern bool InjectTouchInput(uint count, [MarshalAs(UnmanagedType.LPArray), In] POINTER_TOUCH_INFO[] contacts);

        public enum POINTER_FLAG
        {
            //NONE = 0x00000000,
            //NEW = 0x00000001,
            INRANGE = 0x00000002,
            INCONTACT = 0x00000004,
            //FIRSTBUTTON = 0x00000010,
            //SECONDBUTTON = 0x00000020,
            //THIRDBUTTON = 0x00000040,
            //OTHERBUTTON = 0x00000080,
            //PRIMARY = 0x00000100,
            //CONFIDENCE = 0x00000200,
            //CANCELLED = 0x00000400,
            DOWN = 0x00010000,
            UPDATE = 0x00020000,
            UP = 0x00040000,
            //WHEEL = 0x00080000,
            //HWHEEL = 0x00100000
        }

        //int TOUCH_FEEDBACK_DEFAULT = 0x1;
        //int TOUCH_FEEDBACK_INDIRECT = 0x2;
        private int TOUCH_FEEDBACK_NONE = 0x03;


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }


        public enum TouchFlags
        {
            NONE = 0x00000000
        }

        public enum TouchMask
        {
            //NONE = 0x00000000,
            CONTACTAREA = 0x00000001,
            ORIENTATION = 0x00000002,
            PRESSURE = 0x00000004
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct TouchPoint
        {
            public int x;
            public int y;
        }

        public enum POINTER_INPUT_TYPE
        {
            //PT_POINTER = 0x00000001,
            PT_TOUCH = 0x00000002,
            //PT_PEN = 0x00000003,
            //PT_MOUSE = 0x00000004
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTER_INFO
        {
            public POINTER_INPUT_TYPE pointerType;
            public uint pointerId;
            public uint frameId;
            public POINTER_FLAG pointerFlags;
            public IntPtr sourceDevice;
            public IntPtr hwndTarget;
            public TouchPoint ptPixelLocation;
            public TouchPoint ptPixelLocationRaw;
            public TouchPoint ptHimetricLocation;
            public TouchPoint ptHimetricLocationRaw;
            public uint dwTime;
            public uint historyCount;
            public uint inputData;
            public uint dwKeyStates;
            public ulong PerformanceCount;
            public int ButtonChangeType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTER_TOUCH_INFO
        {
            public POINTER_INFO pointerInfo;
            public TouchFlags touchFlags;
            public TouchMask touchMask;
            public RECT rcContact;
            public RECT rcContactRaw;
            public uint orientation;
            public uint pressure;
        }

        #endregion

        #region Hook

        private HookProcDelegate _mouseProc;
        private IntPtr _mouseHook;

        private bool _leftButtonDown = false;
        private bool _mouseMoved = false;


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        const int WH_MOUSE_LL = 14;

        enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        delegate IntPtr HookProcDelegate(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, HookProcDelegate lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool callNextHook = true;

            if (nCode != 0)
            {
                return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
            }

            if (nCode == 0)
            {
                var w = wParam.ToInt32();
                var m = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                
                if (m.flags == 0)
                {
                    //cant use directly m.pt (problem with zoomed screens)
                    POINT pt;
                    GetCursorPos(out pt);

                    switch (w)
                    {
                        case (int)MouseMessages.WM_LBUTTONDOWN:
                            callNextHook = false;
                            _leftButtonDown = true;

                            Down(pt.x, pt.y);
                            Hold();

                            break;
                        case (int)MouseMessages.WM_LBUTTONUP:
                            callNextHook = false;
                            _leftButtonDown = false;

                            Up();

                            break;
                        case (int)MouseMessages.WM_MOUSEMOVE:
                            _mouseMoved = true;

                            if (_leftButtonDown)
                            {
                                Drag(pt.x, pt.y);
                            }

                            break;
                        default:
                            break;
                    }
                }
            }

            if (callNextHook)
                return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
            else
                return new IntPtr(-1);
        }

        #endregion
    }
}
