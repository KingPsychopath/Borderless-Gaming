﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using BorderlessGaming.Properties;
using BorderlessGaming.Utilities;

namespace BorderlessGaming.Common
{
    public class ProcessDetails
    {
        /// <summary>
        ///     Cached list of currently running processes
        /// </summary>
        public static List<ProcessDetails> List = new List<ProcessDetails>();

        public Process Proc = null;
        public string DescriptionOverride = "";
        public string WindowTitle = "";
        //public string WindowClass = ""; // note: this isn't used, currently
        public IntPtr _WindowHandle = IntPtr.Zero;
        public bool Manageable = false;
        public bool MadeBorderless = false;
        private bool NoAccess = false;
        public int MadeBorderlessAttempts = 0;
        public WindowsAPI.WindowStyleFlags OriginalStyleFlags_Standard = 0;
        public WindowsAPI.WindowStyleFlags OriginalStyleFlags_Extended = 0;
        public Rectangle OriginalLocation = new Rectangle();

        // Code commented (but not removed) by psouza4 2015/01/02: there were no references to this method, so no need to compile it and bloat the software.
        //public ProcessDetails(Process p)
        //{
        //    this.Proc = p;

        //    this._WindowHandle = WindowsAPI.Native.GetMainWindowForProcess(this.Proc);
        //    this.WindowTitle = WindowsAPI.Native.GetWindowTitle(this.WindowHandle);
        //    //this.WindowClass = WindowsAPI.Native.GetWindowClassName(this.WindowHandle); // note: this isn't used, currently
        //}

        public ProcessDetails(Process p, IntPtr hWnd)
        {
            this.Proc = p;

            this.WindowHandle = hWnd;
            this.WindowTitle = WindowsAPI.Native.GetWindowTitle(this.WindowHandle);
            //this.WindowClass = WindowsAPI.Native.GetWindowClassName(this.WindowHandle); // note: this isn't used, currently
        }

        // Automatically detects changes to the window handle
        public IntPtr WindowHandle
        {
            get
            {
                try
                {
                    if (this.ProcessHasExited)
                        return IntPtr.Zero;

                    if (!WindowsAPI.Native.IsWindow(this._WindowHandle))
                        this._WindowHandle = WindowsAPI.Native.GetMainWindowForProcess(this.Proc);
                }
                catch { }

                return this._WindowHandle;
            }
            set
            {
                this._WindowHandle = value;
            }
        }

        public bool ProcessHasExited
        {
            get
            {
                try
                {
                    if (this.NoAccess)
                        return false;

                    if (this.Proc != null)
                        return this.Proc.HasExited;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    this.NoAccess = true;

                    return false; // Access is denied
                }
                catch { }

                return true;
            }
        }
        
        public string BinaryName
        {
            get
            {
                try
                {
                    return this.Proc.ProcessName;
                }
                catch { }

                return "<error>";
            }
        }

        public override string ToString() // so that the ListView control knows how to display this object to the user
        {
            try
            {
                if (!string.IsNullOrEmpty(this.DescriptionOverride))
                    return this.DescriptionOverride;

                if (AppEnvironment.SettingValue("ViewAllProcessDetails", false))
                {
                    WindowsAPI.WindowStyleFlags styleCurrentWindow_standard = WindowsAPI.Native.GetWindowLong(this.WindowHandle, WindowsAPI.WindowLongIndex.Style);
                    WindowsAPI.WindowStyleFlags styleCurrentWindow_extended = WindowsAPI.Native.GetWindowLong(this.WindowHandle, WindowsAPI.WindowLongIndex.ExtendedStyle);

                    string extra_details = string.Format(" [{0:X8}.{1:X8}]", (UInt32)styleCurrentWindow_standard, (UInt32)styleCurrentWindow_extended);

                    if (this.WindowTitle.Trim().Length == 0)
                        return this.BinaryName + " [#" + this.Proc.Id.ToString() + "]" + extra_details;

                    return this.WindowTitle.Trim() + " [" + this.BinaryName + ", #" + this.Proc.Id.ToString() + "]" + extra_details;
                }

                if (this.WindowTitle.Trim().Length == 0)
                    return this.BinaryName;

                bool ProcessNameIsDissimilarToWindowTitle = true;
                if (this.WindowTitle_ForComparison.Length >= 5)
                    if (this.BinaryName_ForComparison.Length >= 5)
                        if (this.BinaryName_ForComparison.Substring(0, 5) == this.WindowTitle_ForComparison.Substring(0, 5))
                            ProcessNameIsDissimilarToWindowTitle = false;

                if (ProcessNameIsDissimilarToWindowTitle)
                    return this.WindowTitle.Trim() + " [" + this.BinaryName + "]";

                return this.WindowTitle.Trim();

            }
            catch { }

            return "<error>";
        }

        private string WindowTitle_ForComparison
        {
            get
            {
                return this.WindowTitle.Trim().ToLower().Replace(" ", "").Replace("_", "");
            }
        }

        private string BinaryName_ForComparison
        {
            get
            {
                return this.BinaryName.Trim().ToLower().Replace(" ", "").Replace("_", "");
            }
        }

        // Detect whether or not the window needs border changes
        public bool WindowHasTargetableStyles
        {
            get
            {
                WindowsAPI.WindowStyleFlags styleCurrentWindow_standard = WindowsAPI.Native.GetWindowLong(this.WindowHandle, WindowsAPI.WindowLongIndex.Style);
                WindowsAPI.WindowStyleFlags styleCurrentWindow_extended = WindowsAPI.Native.GetWindowLong(this.WindowHandle, WindowsAPI.WindowLongIndex.ExtendedStyle);

                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.Border) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.DialogFrame) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.ThickFrame) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.SystemMenu) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.MaximizeBox) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.MinimizeBox) > 0) return true;

                if ((styleCurrentWindow_extended | WindowsAPI.WindowStyleFlags.ExtendedDlgModalFrame) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.ExtendedComposited) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.ExtendedWindowEdge) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.ExtendedClientEdge) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.ExtendedLayered) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.ExtendedStaticEdge) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.ExtendedToolWindow) > 0) return true;
                if ((styleCurrentWindow_standard | WindowsAPI.WindowStyleFlags.ExtendedAppWindow) > 0) return true;

                return false;
            }
        }

        public static implicit operator ProcessDetails(Process process)
        {
            for (int i = 0; i < ProcessDetails.List.Count; i++)
                if ((ProcessDetails.List[i].Proc.Id == process.Id) && (ProcessDetails.List[i].Proc.ProcessName == process.ProcessName))
                    return ProcessDetails.List[i];

            return null;
        }

        public static implicit operator ProcessDetails(IntPtr hWnd)
        {
            for (int i = 0; i < ProcessDetails.List.Count; i++)
                if (ProcessDetails.List[i].WindowHandle == hWnd)
                    return ProcessDetails.List[i];

            return null;
        }

        public static implicit operator Process(ProcessDetails pd)
        {
            if (pd == null)
                return null;

            return pd.Proc;
        }

        public static implicit operator IntPtr(ProcessDetails pd)
        {
            if (pd == null)
                return IntPtr.Zero;

            return pd.WindowHandle;
        }
    }
}
