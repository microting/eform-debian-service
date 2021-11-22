using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Security;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microting.eForm;
using Microting.eForm.Dto;
using Microting.eForm.Infrastructure;
using Microting.eForm.Infrastructure.Helpers;
using Microting.WindowsService.BasePn;

namespace MicrotingService
{
    public class ServiceLogic
    {
        #region var

        Tools t = new Tools();
        private string _serviceLocation;

#pragma warning disable 649
        private readonly eFormCore.Core _sdkCore;
#pragma warning restore 649

        [ImportMany(typeof(ISdkEventHandler), AllowRecomposition = true)]
        private IEnumerable<Lazy<ISdkEventHandler>> _eventHandlers;
        #endregion

        //con
        public ServiceLogic()
        {
            try
            {
                LogEvent("Service called");
                {
                    _serviceLocation = "";
                    _sdkCore = new eFormCore.Core();

                    //An aggregate catalog that combines multiple catalogs
                    var catalog = new AggregateCatalog();

                    //Adds all the parts found in the same assembly as the Program class
                    LogEvent("Start loading plugins...");
                    try
                    {
                        string path = Path.Combine(GetServiceLocation(), @"Plugins");
                        Directory.CreateDirectory(path);
                        LogEvent("Path for plugins is : " + path);
                        foreach (string dir in Directory.GetDirectories(path))
                        {
                            if (Directory.Exists(Path.Combine(dir, "net6.0")))
                            {
                                LogEvent("Loading Plugin : " + Path.Combine(dir, "net6.0"));
                                catalog.Catalogs.Add(new DirectoryCatalog(Path.Combine(dir, "net6.0")));
                            } else
                            {
                                LogEvent("Loading Plugin : " + dir);
                                catalog.Catalogs.Add(new DirectoryCatalog(dir));
                            }
                        }
                    } catch (Exception e) {
                        LogException("Something went wrong in loading plugins.");
                        LogException(e.Message);
                    }
                    //Create the CompositionContainer with the parts in the catalog
                    CompositionContainer container = new CompositionContainer(catalog);

                    //Fill the imports of this object
                    try
                    {
                        container.ComposeParts(this);
                    }
                    catch (CompositionException compositionException)
                    {
                        LogException(compositionException.ToString());
                    }
                }
                LogEvent("Service completed");
            }
            catch (Exception ex)
            {
                LogException(t.PrintException("Fatal Exception", ex));
                throw;
            }
        }

        #region public state
        public void Start()
        {

            string connectionString = File.ReadAllText(GetServiceLocation() + "input\\sql_connection_sdkCore.txt").Trim();
            Start(connectionString);
        }

        public void Start(string sdkSqlCoreStr)
        {
            #region start plugins
            try
            {

                foreach (Lazy<ISdkEventHandler> i in _eventHandlers)
                {
                    LogEvent("Trying to start plugin : " + i.Value.GetType().ToString());
                    i.Value.Start(sdkSqlCoreStr, GetServiceLocation());
                    LogEvent(i.Value.GetType().ToString() + " started successfully!");
                }
            }
            catch (Exception e)
            {
                LogException("Start got exception : " + e.Message);
                throw;
            }
            #endregion

            try
            {
                LogEvent("Service Start called");
                {
                    // start debugger?
                    if (File.Exists(GetServiceLocation() + "input\\debug.txt"))
                    {
                        LogEvent("Debugger called");
                        System.Diagnostics.Debugger.Launch();
                    }

                    #region start SDK core
                    #region event connecting
                    try
                    {
                        _sdkCore.HandleEventException -= CoreEventException;
                        _sdkCore.HandleCaseRetrived += _caseRetrived;
                        _sdkCore.HandleCaseCompleted += _caseCompleted;
                        //_sdkCore.HandleNotificationNotFound += _caseCompleted;
                        _sdkCore.HandleeFormProcessedByServer += _eFormProcessedByServer;
                        LogEvent("Core exception events disconnected (if needed)");
                    }
                    catch { }

                    _sdkCore.HandleEventException += CoreEventException;
                    LogEvent("Core exception events connected");
                    #endregion

                    LogEvent("sdkSqlCoreStr, " + sdkSqlCoreStr);

                    DbContextHelper dbContextHelper = new DbContextHelper(sdkSqlCoreStr);

                    var dbContext = dbContextHelper.GetDbContext();

                    while (!dbContext.Database.CanConnect())
                    {
                        LogEvent($"Unable to connect to database (sleeping 5 minutes), using {sdkSqlCoreStr}");
                        Thread.Sleep(300000);
                    }
                    _sdkCore.Start(sdkSqlCoreStr);
                    FixDoneAt(dbContext);

                    LogEvent("SDK Core started");
                    #endregion
                }
                LogEvent("Service Start completed");
            }
            catch (Exception ex)
            {
                LogException(t.PrintException("Fatal Exception", ex));
				throw;
            }
        }

        public void Stop()
        {
            try
            {
                LogEvent("Service Close called");
                {
                    try
                    {

                        foreach (Lazy<ISdkEventHandler> i in _eventHandlers)
                        {
                            LogEvent("Trying to stop plugin : " + i.Value.GetType().ToString());
                            i.Value.Stop(false);
                            LogEvent(i.Value.GetType().ToString() + " stopped successfully!");
                        }
                    }
                    catch (Exception e)
                    {
                        LogException("Stop got exception : " + e.Message);
                    }
                    _sdkCore.Close();
                    LogEvent("SDK Core closed");
                }
                LogEvent("Service Close completed");
            }
            catch (Exception ex)
            {
                LogException(t.PrintException("Fatal Exception", ex));
            }
        }

        public void OverrideServiceLocation(string serviceLocation)
        {
            this._serviceLocation = serviceLocation;
            LogEvent("serviceLocation:'" + serviceLocation + "'");
        }
        #endregion

        #region private
        private string GetServiceLocation()
        {
            if (_serviceLocation != "")
                return _serviceLocation;

            _serviceLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _serviceLocation = Path.GetDirectoryName(_serviceLocation) + "\\";
            }
            else
            {
                _serviceLocation = Path.GetDirectoryName(_serviceLocation) + "/";
            }

            LogEvent("serviceLocation:'" + _serviceLocation + "'");

            return _serviceLocation;
        }

        #region _caseRetrived
        private void _caseRetrived(object sender, EventArgs args)
        {
            try
            {
                foreach (Lazy<ISdkEventHandler> i in _eventHandlers)
                {
                    try
                    {
                        LogEvent("Trying to send event caseRetrieved to plugin : " + i.Value.GetType().ToString());
                        i.Value.eFormRetrived(sender, args);
                    }
                    catch (Exception exception)
                    {
                        LogException("_caseCompleted got exception : " + exception.Message);
                    }
                }
            }
            catch (Exception e)
            {
                LogException("_caseRetrived got exception : " + e.Message);
            }
        }
        #endregion

        #region _caseCompleted
        private void _caseCompleted(object sender, EventArgs args)
        {

            try
            {

                foreach (Lazy<ISdkEventHandler> i in _eventHandlers)
                {
                    try
                    {
                        LogEvent("Trying to send event _caseCompleted to plugin : " + i.Value.GetType().ToString());
                        i.Value.CaseCompleted(sender, args);
                    }
                    catch (Exception exception)
                    {
                        LogException("_caseCompleted got exception : " + exception.Message);
                    }
                }
            }
            catch (Exception e)
            {
                LogException("_caseCompleted got exception : " + e.Message);
            }

        }
        #endregion

        #region _eFormProcessedByServer
        private void _eFormProcessedByServer(object sender, EventArgs args)
        {

            try
            {

                foreach (Lazy<ISdkEventHandler> i in _eventHandlers)
                {
                    try
                    {
                        LogEvent("Trying to send event _caseCompleted to plugin : " + i.Value.GetType().ToString());
                        i.Value.eFormProcessed(sender, args);
                    }
                    catch (Exception exception)
                    {
                        LogException("_caseCompleted got exception : " + exception.Message);
                    }
                }
            }
            catch (Exception e)
            {
                LogException("_caseCompleted got exception : " + e.Message);
            }

        }
        #endregion

        #region _caseNotFound
        private void _caseNoFound(object sender, EventArgs args)
        {

            NoteDto trigger = (NoteDto)sender;
        }
        #endregion

        protected String GetServiceName()
        {
            // Calling System.ServiceProcess.ServiceBase::ServiceNamea allways returns
            // an empty string,
            // see https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=387024

            // So we have to do some more work to find out our service name, this only works if
            // the process contains a single service, if there are more than one services hosted
            // in the process you will have to do something else

            int processId = Process.GetCurrentProcess().Id;
            String query = "SELECT * FROM Win32_Service where ProcessId = " + processId;
            System.Management.ManagementObjectSearcher searcher =
                new System.Management.ManagementObjectSearcher(query);

            foreach (System.Management.ManagementObject queryObj in searcher.Get())
            {
                return queryObj["Name"].ToString();
            }

            throw new Exception("Can not get the ServiceName");
        }

        private void CoreEventException(object sender, EventArgs args)
        {
            //DOSOMETHING: changed to fit your wishes and needs
            Exception ex = (Exception)sender;
        }

        private void LogEvent(string appendText)
        {
            try
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("[DBG] " + appendText);
                Console.ForegroundColor = oldColor;
            }
            catch
            {
            }
        }

        private void LogException(string appendText)
        {
            try
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERR] " + appendText);
                Console.ForegroundColor = oldColor;
            }
            catch
            {

            }
        }

        private static void FixDoneAt(MicrotingDbContext dbContext)
        {
            var cases = dbContext.Cases.Where(x => x.DoneAtUserModifiable == null).ToList();

            foreach (Microting.eForm.Infrastructure.Data.Entities.Case theCase in cases)
            {
                theCase.DoneAtUserModifiable = theCase.DoneAt;
                theCase.Update(dbContext).GetAwaiter().GetResult();
            }
        }
        #endregion
    }
}