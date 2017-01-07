using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace DesktopWPFAppLowLevelKeyboardHook
{
    public class LowLevelKeyboardListener
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;

        //imported

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public event EventHandler<KeyPressedArgs> OnKeyPressed;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;


        public LowLevelKeyboardListener()
        {
            _proc = HookCallback;
        }

        public void HookKeyboard()
        {
            _hookID = SetHook(_proc);
        }

        public void UnHookKeyboard()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                int vkCodeDown = Marshal.ReadInt32(lParam);

                if (!(vkCodeDown >= 19 && vkCodeDown <= 31 || vkCodeDown >= 33 && vkCodeDown <= 46 || vkCodeDown >= 91 && vkCodeDown <= 145) || vkCodeDown == 113)
                {
                    if (OnKeyPressed != null)
                    {
                        OnKeyPressed(this, new KeyPressedArgs(KeyInterop.KeyFromVirtualKey(vkCodeDown), true));
                    }
                }
            }
            
            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)

            {
                int vkCodeReleased = Marshal.ReadInt32(lParam);

                if (OnKeyPressed != null)
                {
                    OnKeyPressed(this, new KeyPressedArgs(KeyInterop.KeyFromVirtualKey(vkCodeReleased), false));
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }
    public class KeyPressedArgs : EventArgs
    {
        
        private List<Key> modifierKeys = new List<Key> { Key.LeftAlt, Key.RightAlt, Key.LeftShift, Key.RightShift, Key.LeftCtrl, Key.RightCtrl };

        public List<Key> KeyPressed { get; private set; }

        public List<Key> Modifiers { get; private set; }
        public bool downPress;
        public bool lastKeyisModifier;

        public KeyPressedArgs(Key key, bool isDown)
        {
            KeyPressed = KeyList.returnKeyList();
            Modifiers = KeyList.returnModifierList();
            

            if (isDown)
            {
                downPress = true;
                

                if (!modifierKeys.Contains(key))
                {
                    lastKeyisModifier = false;
                    KeyList.recordKey(key);
                }

                if (modifierKeys.Contains(key))
                {
                    lastKeyisModifier = true;
                    KeyList.recordModifier(key);
                }
            }
            else
            {
                downPress = false;
                if (!modifierKeys.Contains(key))
                {
                    KeyList.removeKey(key);
                }
                if (modifierKeys.Contains(key))
                {
                    KeyList.removeModifier(key);

                }
            }
        }

    }

    static class KeyList
    {
        static List<Key> _list; // Static List instance
        static List<Key> modifierList; // Static List instance
        static KeyList()
        {
            //
            // Allocate the list.
            //
            _list = new List<Key>();
            modifierList = new List<Key>();
        }

        public static void recordKey(Key value)
        {
            //
            // Record this value in the list.
            //
            if(!_list.Contains(value))
            {
                _list.Add(value);
            }
            
        }


        public static void recordModifier(Key value)
        {
            //
            // Remove this value in the list.
            //
            if(!modifierList.Contains(value))
            {
                modifierList.Add(value);
            }
            
        }


        public static void removeKey(Key value)
        {
            //
            // Remove this value in the list.
            //
            _list.Remove(value);
        }


        public static void removeModifier(Key value)
        {
            //
            // Record this value in the list.
            //
            modifierList.Remove(value);
        }



        public static void Display()
        {
            //
            // Write out the results.
            //
            foreach (var value in _list)
            {
                Console.Write(value);
            }

            foreach (var value in modifierList)
            {
                Console.Write(value);
            }
        }

        public static void removeKeys()
        {
            _list.Clear();
        }

        public static void removeModifiers()
        {
            modifierList.Clear();

        }
        public static List<Key> returnKeyList()
        {
            return _list;
        }

        public static List<Key> returnModifierList()
        {
            return modifierList;
        }
    }
}