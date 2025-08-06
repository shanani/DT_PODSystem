
//Static files////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Use CDN to refrences to the theme files and plugins
No need for node.js and gulp

//Notifications//////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Popup Sweetalert///////////////////////////////////////////////////////////////////////////////////////////////////////
Server Side:
 TempData.Success("Done! Great job!");
 TempData.Warning("Please check the data entered.");
 TempData.Error("An error occured!");
 TempData.Info("Nice Day :)");
 TempData.Success("Done! Great job!", "Yeaaa!", 0); //Custom options

Client Side:
 alert.success("Done! Great job!");
 alert.warning("please check the data entry");
 alert.error("An error occured!");
 alert.info("Nice Day :)",{time:0,title:"Welcome back"}); //Custom options


 //Toast Notifications///////////////////////////////////////////////////////////////////////////////////////////////////////
 Server Side:
 TempData.Success("Done! Great job!", popup:false);
 TempData.Warning("Please check the data entered.", popup:false);
 TempData.Error("An error occured!", popup:false);
 TempData.Info("Nice Day :)");
 TempData.Success("Done! Great job!", "Yeaaa!", 0, popup:false); //Custom options

Client Side:
 alert.success("Done! Great job!",{popup:false});
 alert.warning("please check the data entry",{popup:false});
 alert.error("An error occured!",{popup:false});
 alert.info("Nice Day :)",{time:0,title:"Welcome back",popup:false}); //Custom options


//Inline Alert///////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    <div class="alert alert-dark">
            <h5><i class="fa fa-info-circle"></i> Alert Header</h5>
            <p>Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin commodo. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.</p>
        </div>
        <div class="alert alert-danger">
            <h5><i class="fa fa-info-circle"></i> Alert Header</h5>
            <p>Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin commodo. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.</p>
        </div>
        <div class="alert alert-success">
            <h5><i class="fa fa-info-circle"></i> Alert Header</h5>
            <p>Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin commodo. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.</p>
        </div>
        <div class="alert alert-warning">
            <h5><i class="fa fa-info-circle"></i> Alert Header</h5>
            <p>Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin commodo. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.</p>
        </div>
        <div class="alert alert-info">
            <h5><i class="fa fa-info-circle"></i> Alert Header</h5>
            <p>Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin commodo. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.</p>
        </div>
        <div class="alert alert-green">
            <h5><i class="fa fa-info-circle"></i> Alert Header</h5>
            <p>Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin commodo. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.</p>
        </div>

//Send Emial Example/////////////////////////////////////////////////////////////////////////////////////////////////////////
 
 1) Inject the Email Service to your conroller :

 public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly EmailService _emailService;
        public HomeController(ILogger<HomeController> logger, EmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

 
 2) Use the Email Service :
 
 var emai = new EmailModel()
            {
                RecipientEmail = Util.GetCurrentUser().Email,
                Subject = "Test Email",
                Body = $"Hi {Util.GetCurrentUser().User.FirstName},<br /> This is a test email.<br /><br /><b>Best Regards,<br />Development Team</b>"
            };
  await _emailService.SendEmailAsync(emai);

  


///////////////////////////////////////
||  Best Regards,                    ||
||                                   ||
||  stc Ops Development Team         ||
//////////////////////////////////////


 


dotnet ef migrations add InitialCreate --context ApplicationDbContext --output-dir "Migrations/ApplicationDb"
  

 
dotnet ef migrations add InitialCreate --context SecurityDbContext --output-dir "Migrations/SecurityDb"
 
 
dotnet ef database drop --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext  
dotnet ef database update --context SecurityDbContext
dotnet run seed  