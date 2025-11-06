using Microsoft.AspNetCore.SignalR;

namespace Order_Tracking.Hubs
{ 
    public class OrdersHub :Hub
    {
        // هنا ممكن نخلي Worker يرسل أي رسالة للـ Clients
        //ده Hub بسيط جدًا، مجرد نقطة استقبال الرسائل وإرسالها لكل المتصلين.
    }
}
