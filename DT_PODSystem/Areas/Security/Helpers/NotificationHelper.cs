using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;

namespace DT_PODSystem.Areas.Security.Helpers
{
    public static class NotificationHelper
    {
        private const string NotificationsKey = "Notifications";

        public static void AddNotification(this ITempDataDictionary tempData, string type, string message, string title = "", int? duration = null, bool popup = false)
        {
            var notifications = tempData.ContainsKey(NotificationsKey)
                ? JsonConvert.DeserializeObject<List<Notification>>(tempData[NotificationsKey] as string)
                : new List<Notification>();
            notifications.Add(new Notification
            {
                Type = type,
                Message = message,
                Title = title,
                Duration = duration,
                popup = popup
            });
            tempData[NotificationsKey] = JsonConvert.SerializeObject(notifications);
        }


        public static void Success(this ITempDataDictionary tempData, string message, string title = "", int? duration = null, bool popup = true)
        {
            tempData.AddNotification("success", message, title, duration, popup);
        }

        public static void Error(this ITempDataDictionary tempData, string message, string title = "", int? duration = null, bool popup = true)
        {
            tempData.AddNotification("error", message, title, duration, popup);
        }

        public static void Warning(this ITempDataDictionary tempData, string message, string title = "", int? duration = null, bool popup = true)
        {
            tempData.AddNotification("warning", message, title, duration, popup);
        }

        public static void Info(this ITempDataDictionary tempData, string message, string title = "", int? duration = null, bool popup = true)
        {
            tempData.AddNotification("info", message, title, duration, popup);  // Changed from "normal" to "info"
        }
        private class Notification
        {
            public string Type { get; set; }
            public string Message { get; set; }
            public string Title { get; set; }
            public int? Duration { get; set; }
            public bool popup { get; set; }
        }
    }
}