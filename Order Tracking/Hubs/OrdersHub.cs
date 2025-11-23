using Microsoft.AspNetCore.SignalR;

namespace Order_Tracking.Hubs
{ 
    public class OrdersHub : Hub
    {
       
        public async Task JoinGroup(string userId)
        {
            string groupName = $"user-{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }
        public async Task JoinDeliveryGroup() 
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Delivery");
        }


    }
}
