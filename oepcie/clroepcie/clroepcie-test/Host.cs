﻿using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Threading;
using oe;
using System.Runtime.InteropServices;

namespace clroepcie_test
{

class DataProcessor
{
    private oe.Context ctx;
    public bool quit = false;
    public bool display = true;
    public bool display_clock = true;

    public DataProcessor(oe.Context ctx)
    {
        this.ctx = ctx;
    }

    public void CaptureData()
    {
        int rc = 0;
        while (rc == 0 && !quit)  {

            var frame = ctx.ReadFrame();

            if (display_clock)
                Console.WriteLine("\tSample: {0}\n", frame.Time());

            if (display) {
                foreach (var idx in frame.DeviceIndices) {

                    var dat = frame.Data<ushort>(idx);
                    Console.WriteLine("\tDev: {0} ({1})", idx, ctx.DeviceMap[idx].id);
                    Console.WriteLine("\t[{0}]", String.Join(", ",dat));
                }
            }

        }
    }
}

class Host
{
    static void Main(string[] args)
    {
        // Ger version
        var ver = oe.lib.oepcie.LibraryVersion;
        Console.WriteLine("Using liboepcie version: " + ver);

        try
        {
            // Open context
            using (var ctx = new oe.Context("/tmp/rat128_config",
                                            "/tmp/rat128_read",
                                            "/tmp/rat128_signal"))
            {

                Console.WriteLine("Found the following devices:" );
                foreach (var elem in ctx.DeviceMap) {

                    var index = elem.Key;
                    var device = elem.Value;

                    Console.WriteLine("\t{0}) ID: {1}, Read size: {2}",
                    index,
                    device.id,
                    device.read_size);
                }

                // Start acqusisition
                ctx.Start();

                // See how big max frame is
                int frame_size = ctx.GetOption(Context.Option.MAXREADFRAMESIZE);
                Console.WriteLine("Max read frame size: " + frame_size);

                // See if we are running
                int hz = ctx.GetOption(Context.Option.SYSCLKHZ);
                Console.WriteLine("System clock frequency: " + hz);

                // See if we are running
                int running = ctx.GetOption(Context.Option.RUNNING);
                Console.WriteLine("Running state: " + running);

                // Start processor in background
                var processor = new DataProcessor(ctx);
                var proc_thread = new Thread(new ThreadStart(processor.CaptureData));
                proc_thread.Start();

                int c = 's';
                while (c != 'q') {

                    Console.WriteLine("Enter a command and press enter:");
                    Console.WriteLine("\tc - toggle clock display");
                    Console.WriteLine("\td - toggle display");
                    Console.WriteLine("\tp - toggle stream pause");
                    Console.WriteLine("\tr - enter register command");
                    Console.WriteLine("\tq - quit");
                    Console.WriteLine(">>> ");

                    var cmd = Console.ReadLine();
                    c = cmd[0];

                    //if (c == 'p') {
                    //    running = (running == 1) ? 0 : 1;
                    //    oe_reg_val_t run = running;
                    //    rc = oe_set_opt(ctx, OE_RUNNING, &run, sizeof(run));
                    //    if (rc) {
                    //        printf("Error: %s\n", oe_error_str(rc));
                    //    }
                    //    printf("Paused\n");
                    //} else
                    if (c == 'c') {
                        processor.display_clock = !processor.display_clock;
                    } else if (c == 'd') {
                        processor.display = !processor.display;
                    }
                    //else if (c == 'r') {

                    //    printf("Enter dev_idx reg_addr reg_val\n");
                    //    printf(">>> ");

                    //    // Read the command
                    //    char *buf = NULL;
                    //    size_t len = 0;
                    //    rc = getline(&buf, &len, stdin);
                    //    if (rc == -1) { printf("Error: bad command\n"); continue;}

                    //    // Parse the command string
                    //    long values[3];
                    //    rc = parse_reg_cmd(buf, values);
                    //    if (rc == -1) { printf("Error: bad command\n"); continue;}
                    //    free(buf);

                    //    size_t dev_idx = (size_t)values[0];
                    //    oe_reg_addr_t addr = (oe_reg_addr_t)values[1];
                    //    oe_reg_val_t val = (oe_reg_val_t)values[2];

                    //    oe_write_reg(ctx, dev_idx, addr, val);
                    //}
                }

                // Join data and signal threads
                processor.quit = true;


                // Reset the hardware
                //oe_reg_val_t reset = 1;
                //rc = oe_set_opt(ctx, OE_RESET, &reset, sizeof(reset));
                //if (rc) { printf("Error: %s\n", oe_error_str(rc)); }
                //assert(!rc && "Register write failure.");


                proc_thread.Join();

            } // ctx.Dispose() is called.

        }
        catch (OEException ex)
        {
            Console.Error.WriteLine("liboepcie failed with the following error: " + ex.ToString());
            Console.Error.WriteLine("Current errno: " + Marshal.GetLastWin32Error());
        }
    }
}
}