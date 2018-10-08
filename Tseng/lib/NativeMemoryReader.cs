using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class NativeMemoryReader : IDisposable
{


#region "API Definitions"

    [DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
    static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    /*
     * <DllImportAttribute("kernel32.dll", EntryPoint:="ReadProcessMemory", SetLastError:=True)> _
    Private Shared Function ReadProcessMemory(<InAttribute()> ByVal hProcess As System.IntPtr, 
    <InAttribute()> ByVal lpBaseAddress As System.IntPtr, <Out()> ByVal lpBuffer As Byte(),
    ByVal nSize As UInteger, 
    <OutAttribute()> ByRef lpNumberOfBytesRead As UInteger) As <System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)> Boolean
   
     */
    [DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    static extern   bool ReadProcessMemory([In] IntPtr hProcess, [In] IntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint nSize, [Out] uint lpNumberOfBytesRead);

    [return: MarshalAsAttribute(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    static extern bool CloseHandle(IntPtr hObject);
  

#endregion



    private Process _TargetProcess = null;
    private IntPtr _TargetProcessHandle = IntPtr.Zero;
    const uint PROCESS_VM_READ = 16;
    const uint PROCESS_QUERY_INFORMATION = 1024;




    /// <summary>
    ///     ''' The process that memory will be read from when ReadMemory is called
    ///     ''' </summary>
    public Process TargetProcess
    {
        get
        {
            return _TargetProcess;
        }
    }

    /// <summary>
    ///     ''' The handle to the process that was retrieved during the constructor or the last
    ///     ''' successful call to the Open method
    ///     ''' </summary>
    public IntPtr TargetProcessHandle
    {
        get
        {
            return _TargetProcessHandle;
        }
    }




    /// <summary>
    ///     ''' Reads the specified number of bytes from an address in the process's memory.
    ///     ''' All memory in the specified range must be available or the method will fail.
    ///     ''' Returns Nothing if the method fails for any reason
    ///     ''' </summary>
    ///     ''' <param name="MemoryAddress">The address in the process's virtual memory to start reading from</param>
    ///     ''' <param name="Count">The number of bytes to read</param>
    public byte[] ReadMemory(IntPtr MemoryAddress, int Count)
    {
        if (_TargetProcessHandle == IntPtr.Zero)
            this.Open();
        byte[] Bytes = new byte[Count + 1];
        uint read;
        bool Result = ReadProcessMemory(_TargetProcessHandle, MemoryAddress, Bytes, System.Convert.ToUInt32(Count), 0);
        if (Result)
            return Bytes;
        else
            return null;
    }

    /// <summary>
    ///     ''' Gets a handle to the process specified in the TargetProcess property.
    ///     ''' A handle is automatically obtained by the constructor of this class but if the Close
    ///     ''' method has been called to close a previously obtained handle then another handle can
    ///     ''' be obtained by calling this method. If a handle has previously been obtained and Close has
    ///     ''' not been called yet then an exception will be thrown.
    ///     ''' </summary>
    public void Open()
    {
        if (_TargetProcess == null)
            throw new ApplicationException("Process not found");
        if (_TargetProcessHandle == IntPtr.Zero)
        {
            _TargetProcessHandle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, true, System.Convert.ToUInt32(_TargetProcess.Id));
            if (_TargetProcessHandle == IntPtr.Zero)
                throw new ApplicationException("Unable to open process for memory reading. The last error reported was: " + new System.ComponentModel.Win32Exception().Message);
        }
        else
            throw new ApplicationException("A handle to the process has already been obtained, " + "close the existing handle by calling the Close method before calling Open again");
    }

    /// <summary>
    ///     ''' Closes a handle that was previously obtained by the constructor or a call to the Open method
    ///     ''' </summary>
    public void Close()
    {
        if (_TargetProcessHandle != IntPtr.Zero)
        {
            bool Result = CloseHandle(_TargetProcessHandle);
            if (!Result)
                throw new ApplicationException("Unable to close process handle. The last error reported was: " + new System.ComponentModel.Win32Exception().Message);
            _TargetProcessHandle = IntPtr.Zero;
        }
    }




    /// <summary>
    ///     ''' Creates a new instance of the NativeMemoryReader class and attempts to get a handle to the
    ///     ''' process that is to be read by calls to the ReadMemory method.
    ///     ''' If a handle cannot be obtained then an exception is thrown
    ///     ''' </summary>
    ///     ''' <param name="ProcessToRead">The process that memory will be read from</param>
    public NativeMemoryReader(Process ProcessToRead)
    {
        if (ProcessToRead == null)
            throw new ArgumentNullException("ProcessToRead");
        _TargetProcess = ProcessToRead;
        this.Open();
    }




    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (_TargetProcessHandle != IntPtr.Zero)
            {
                try
                {
                    CloseHandle(_TargetProcessHandle);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error closing handle - " + ex.Message);
                }
            }
        }
        this.disposedValue = true;
    }

    ~NativeMemoryReader()
    {
        Dispose(false);
    }

    /// <summary>
    ///     ''' Releases resources and closes any process handles that are still open
    ///     ''' </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
