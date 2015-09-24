using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Windows.Forms;


namespace Vertigo
{
    // http://www.doogal.co.uk/exception.php

    /// <summary>
    /// Enumerated type used to decide how exceptions will be logged
    /// </summary>
    public enum ExceptionLogType
    {
        /// <summary>Exception will be logged to a text file</summary>
        TextFile,
        /// <summary>Exception will be logged to the application event log</summary>
        EventLog,
        /// <summary>Exception will be logged via email</summary>
        Email,
        /// <summary>Log to a website</summary>
        WebSite
    }

    /// <summary>
    /// Class to log unhandled exceptions to a text file, event log or email
    /// </summary>
    public class ExceptionLogger
    {
        /// <summary>
        /// Creates a new instance of the ExceptionLogger class
        /// </summary>
        public ExceptionLogger()
        {
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);
        }

        private string GetExceptionStack(Exception e)
        {
            StringBuilder message = new StringBuilder();
            message.Append(e.Message);
            while (e.InnerException != null)
            {
                e = e.InnerException;
                message.Append(Environment.NewLine);
                message.Append(e.Message);
            }

            return message.ToString();
        }

        delegate void LogExceptionDelegate(Exception e);

        private void HandleException(Exception e)
        {
            switch (logType)
            {
                case ExceptionLogType.WebSite:
                case ExceptionLogType.Email:
                    if (MessageBox.Show("An unexpected error occurred - " + e.Message +
                      ". Do you want to submit an error report?", "Error", MessageBoxButtons.YesNo) == DialogResult.No)
                        return;
                    break;
                case ExceptionLogType.EventLog:
                    MessageBox.Show("An unexpected error occurred - " + e.Message +
                      ". It will be logged to the event log", "Error");
                    break;
                case ExceptionLogType.TextFile:
                    MessageBox.Show("An unexpected error occurred - " + e.Message +
                      ". It will be logged to the text file 'BugReport.txt'", "Error");
                    break;
                default:
                    Debug.Assert(false, "Unrecognised log type - " + logType.ToString());
                    break;
            }

            LogExceptionDelegate logDelegate = new LogExceptionDelegate(LogException);
            logDelegate.BeginInvoke(e, new AsyncCallback(LogCallBack), null);
        }

        // Event handler that will be called when an unhandled
        // exception is caught
        private void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            // Log the exception to a file
            HandleException(e.Exception);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException((Exception)e.ExceptionObject);
        }

        private void LogCallBack(IAsyncResult result)
        {
            LogExceptionDelegate logDelegate = (LogExceptionDelegate)((AsyncResult)result).AsyncDelegate;
            logDelegate.EndInvoke(result);
        }

        private ExceptionLogType logType;
        /// <summary>
        /// Specifies what type of logging will occur
        /// </summary>
        public ExceptionLogType LogType
        {
            get { return logType; }
            set { logType = value; }
        }

        /// 
        /// writes exception details to a log file
        /// 
        private void LogException(Exception exception)
        {
            DateTime now = System.DateTime.Now;
            StringBuilder error = new StringBuilder();

            error.Append("Date:              " +
              System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + Environment.NewLine);
            error.Append("Computer name:     " +
              SystemInformation.ComputerName + Environment.NewLine);
            error.Append("User name:         " +
              SystemInformation.UserName + Environment.NewLine);
            error.Append("OS:                " +
              System.Environment.OSVersion.ToString() + Environment.NewLine);
            error.Append("Culture:           " +
              CultureInfo.CurrentCulture.Name + Environment.NewLine);
            Process[] systemProcesses = Process.GetProcessesByName("System");
            if (System.Environment.OSVersion.Version.Major < 6)
            {
                if (systemProcesses.Length > 0)
                    error.Append("System up time:    " +
                      (DateTime.Now - systemProcesses[0].StartTime).ToString() + Environment.NewLine);
            }
            error.Append("App up time:       " +
              (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString() + Environment.NewLine);
            error.Append("Exception class:   " +
              exception.GetType().ToString() + Environment.NewLine);
            error.Append("Exception message: " +
              GetExceptionStack(exception) + Environment.NewLine);

            error.Append(Environment.NewLine);
            error.Append("Stack Trace:");
            error.Append(Environment.NewLine);
            error.Append(exception.StackTrace);
            error.Append(Environment.NewLine);
            error.Append(Environment.NewLine);
            error.Append("Loaded Modules:");
            error.Append(Environment.NewLine);
            Process thisProcess = Process.GetCurrentProcess();
            foreach (ProcessModule module in thisProcess.Modules)
            {
                error.Append(module.FileName + " " + module.FileVersionInfo.FileVersion);
                error.Append(Environment.NewLine);
            }
            error.Append(Environment.NewLine);
            error.Append(Environment.NewLine);
            error.Append(Environment.NewLine);

            switch (logType)
            {
                case ExceptionLogType.TextFile:
                    LogToFile(error.ToString());
                    break;
                case ExceptionLogType.EventLog:
                    LogToEventLog(error.ToString());
                    break;
                case ExceptionLogType.Email:
                    LogToEmail(error.ToString());
                    break;
                case ExceptionLogType.WebSite:
                    LogToWebsite(error.ToString());
                    break;
                default:
                    Debug.Assert(false, "Unrecognised logType - " + logType.ToString());
                    break;
            }
        }

        #region email support

        private string emailTo;
        /// <summary>
        /// Specifies the email address that the exception information will be sent to
        /// </summary>
        public string EmailTo
        {
            get
            {
                return emailTo;
            }
            set
            {
                emailTo = value;
            }
        }

        private string emailFrom;
        /// <summary>
        /// Specifies the email address that the exception information will be sent from
        /// </summary>
        public string EmailFrom
        {
            get
            {
                return emailFrom;
            }
            set
            {
                emailFrom = value;
            }
        }

        private string emailServer;
        /// <summary>
        /// Specifies the email server that the exception information email will be sent via
        /// </summary>
        public string EmailServer
        {
            get
            {
                return emailServer;
            }
            set
            {
                emailServer = value;
            }
        }

        private void LogToEmail(string error)
        {
            MailMessage message = new MailMessage(emailFrom, emailTo, "Unhandled exception report", error);
            SmtpClient client = new SmtpClient(emailServer);
            // Add credentials if the SMTP server requires them.
            client.Credentials = CredentialCache.DefaultNetworkCredentials;
            client.Send(message);
        }

        #endregion

        private void LogToEventLog(string error)
        {
            EventLog log = new EventLog("Application");
            log.Source = Assembly.GetExecutingAssembly().ToString();
            log.WriteEntry(error, EventLogEntryType.Error);
        }

        private void LogToFile(string error)
        {
            string filename = Path.GetDirectoryName(Application.ExecutablePath);
            filename += "\\BugReport.txt";

            ArrayList data = new ArrayList();

            lock (this)
            {
                if (File.Exists(filename))
                {
                    using (StreamReader reader = new StreamReader(filename))
                    {
                        string line = null;
                        do
                        {
                            line = reader.ReadLine();
                            data.Add(line);
                        }
                        while (line != null);
                    }
                }

                // truncate the file if it's too long
                int writeStart = 0;
                if (data.Count > 500)
                    writeStart = data.Count - 500;

                using (StreamWriter stream = new StreamWriter(filename, false))
                {
                    for (int i = writeStart; i < data.Count; i++)
                    {
                        stream.WriteLine(data[i]);
                    }

                    stream.Write(error);
                }
            }
        }

        private string url;
        /// <summary>
        /// Gets or sets the URL that will be used when posting an error to a website.
        /// </summary>
        public string Url
        {
            get
            {
                return url;
            }
            set
            {
                url = value;
            }
        }

        private string queryString;
        /// <summary>
        /// Gets or sets the format of the query string that will be used when posting an error to a website. 
        /// e.g error={0}
        /// </summary>
        public string QueryString
        {
            get
            {
                return queryString;
            }
            set
            {
                queryString = value;
            }
        }

        private void LogToWebsite(string error)
        {
            //Uri uri = new Uri(url);
            //HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            //httpWebRequest.Method = "POST";
            //httpWebRequest.ContentType = "application/x-www-form-urlencoded";

            //Encoding encoding = Encoding.Default;

            //string parameters = string.Format(queryString, HttpUtility.UrlEncode(error));

            //// get length of request (may well be a better way to do this)
            //MemoryStream memStream = new MemoryStream();
            //StreamWriter streamWriter = new StreamWriter(memStream, encoding);
            //streamWriter.Write(parameters);
            //streamWriter.Flush();
            //httpWebRequest.ContentLength = memStream.Length;
            //streamWriter.Close();

            //Stream stream = httpWebRequest.GetRequestStream();
            //streamWriter = new StreamWriter(stream, encoding);
            //streamWriter.Write(parameters);
            //streamWriter.Close();

            //using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            //using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
            //{
            //    streamReader.ReadToEnd();
            //}
        }
    }
}
