using KrishiSahayAI.Data;
using KrishiSahayAI.Models;
using KrishiSahayAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;


namespace KrishiSahayAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDb _db;
        private readonly ICropKnowledgeService _cropKnowledge;
        private readonly IGeminiService _gemini;
        private readonly IWeatherService _weatherService;
        private readonly IWebHostEnvironment _environment;
        [Authorize]
        [HttpGet]
        public IActionResult AddCrop(int farmId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm = _db.GetFarm(
                farmId,
                userId);

            if (farm == null)
            {
                return NotFound();
            }

            var crops = _db.GetFarmCrops(
                farmId,
                userId);

            var areaInfo =
                CalculateFarmArea(
                    farm,
                    crops);

            ViewBag.Farm = farm;

            ViewBag.AllocatedArea =
                areaInfo.Allocated;

            ViewBag.RemainingArea =
                areaInfo.Remaining;

            ViewBag.AreaUnit =
                farm.LandUnit;

            ViewBag.AreaCanBeConverted =
                areaInfo.CanConvert;


            return View(
                new FarmCrop
                {
                    FarmId = farmId,

                    UserId = userId,

                    AreaUnit =
                        AreaUnitConverter.IsSameUnit(
                            farm.LandUnit,
                            "Bigha")
                            ? "Bigha"
                            : "Acre"
                });
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCrop(FarmCrop model)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var farm =
                _db.GetFarm(
                    model.FarmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }


            // -----------------------------
            // BASIC VALIDATION
            // -----------------------------

            if (string.IsNullOrWhiteSpace(
                model.CropName))
            {
                ModelState.AddModelError(
                    nameof(model.CropName),
                    "Please enter a crop name.");
            }


            if (model.Area <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.Area),
                    "Please enter a crop area greater than 0.");
            }


            // -----------------------------
            // FARM AREA VALIDATION
            // -----------------------------

            var existingCrops =
                _db.GetFarmCrops(
                    model.FarmId,
                    userId);


            var areaInfo =
                CalculateFarmArea(
                    farm,
                    existingCrops);


            if (!TryCalculateCropAreaAgainstFarm(
                farm,
                existingCrops,
                model.Area,
                model.AreaUnit,
                out var allocatedAfterAdd,
                out var remainingAfterAdd,
                out var areaError))
            {
                ModelState.AddModelError(
                    nameof(model.Area),
                    areaError);
            }
            else if (remainingAfterAdd < -0.000001)
            {
                ModelState.AddModelError(
                    nameof(model.Area),
                    $"Farm area exceeded. " +
                    $"Your farm has only " +
                    $"{Math.Max(0, areaInfo.Remaining):0.##} " +
                    $"{farm.LandUnit} available, " +
                    $"but you are trying to allocate " +
                    $"{model.Area:0.##} " +
                    $"{model.AreaUnit}.");
            }


            // -----------------------------
            // RETURN FORM IF INVALID
            // -----------------------------

            if (!ModelState.IsValid)
            {
                ViewBag.Farm = farm;

                ViewBag.AllocatedArea =
                    areaInfo.Allocated;

                ViewBag.RemainingArea =
                    areaInfo.Remaining;

                ViewBag.AreaUnit =
                    farm.LandUnit;

                ViewBag.AreaCanBeConverted =
                    areaInfo.CanConvert;

                return View(model);
            }


            // -----------------------------
            // SAVE
            // -----------------------------

            model.UserId =
                userId;

            model.CreatedAt =
                DateTime.UtcNow;


            _db.SaveFarmCrop(model);

            // =====================================================
            // COMPLETE PLANTING MILESTONE
            // A crop with a sowing date means planting has started.
            // =====================================================
            // =====================================================
            // COMPLETE PLANTING MILESTONE
            // A crop with a sowing date means planting has started.
            // =====================================================

            if (model.SowingDate.HasValue)
            {
                var plantingActivity =
                    _db.GetActivities(model.FarmId)
                        .FirstOrDefault(
                            a =>
                                a.Title ==
                                "Plant your crop");

                // Safety for older farms that were created before
                // the planting milestone was added.
                if (plantingActivity == null)
                {
                    _db.AddActivity(
                        new FarmActivity
                        {
                            FarmProfileId = model.FarmId,

                            Title = "Plant your crop",

                            Description =
                                "Add your crop and record the sowing date to begin your crop journey.",

                            ActivityDate =
                                DateTime.Today.AddDays(6),

                            Category = "Planting"
                        });

                    plantingActivity =
                        _db.GetActivities(model.FarmId)
                            .FirstOrDefault(
                                a =>
                                    a.Title ==
                                    "Plant your crop");
                }

                if (plantingActivity != null)
                {
                    _db.UpdateActivityCompletion(
                        plantingActivity.Id,
                        model.FarmId,
                        true);
                }
            }
            return RedirectToAction(
                nameof(MyCrops),
                new
                {
                    farmId = model.FarmId
                });
        }
        public HomeController(
            AppDb db,
            ICropKnowledgeService cropKnowledge,
            IGeminiService gemini,
            IWeatherService weatherService,
            IWebHostEnvironment environment)
        {
            _db = db;
            _cropKnowledge = cropKnowledge;
            _gemini = gemini;
            _weatherService = weatherService;
            _environment = environment;
        }

        // =========================================================
        // HOME
        // =========================================================
        [Authorize]
        public IActionResult MyFarms()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farms = _db.GetFarmsByUserId(userId);

            return View(farms);
        }
        [Authorize]
        public IActionResult MyCrops(int farmId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            var crops =
                _db.GetFarmCrops(
                    farmId,
                    userId);

            var areaInfo =
                CalculateFarmArea(
                    farm,
                    crops);

            var model =
                new MyCropsViewModel
                {
                    Farm = farm,

                    Crops = crops,

                    AllocatedArea =
                        areaInfo.Allocated,

                    RemainingArea =
                        areaInfo.Remaining,

                    AreaDisplayUnit =
                        farm.LandUnit,

                    AreaCanBeConverted =
                        areaInfo.CanConvert
                };

            return View(model);
        }
        public IActionResult Index()
        {
            return View();
        }
        //----------


        // =========================================================
        // ONBOARDING
        // =========================================================

        // AI Crop Doctor entry flow:
        //
        // NOT LOGGED IN
        //     -> Show Create Your Farm
        //
        // LOGGED IN + NO FARM
        //     -> Show Create Your Farm
        //
        // LOGGED IN + ONE FARM
        //     -> Open View My Crops directly
        //
        // LOGGED IN + MULTIPLE FARMS
        //     -> Open My Farms so the user can choose a farm
        //
        // This keeps existing users from being asked to fill the
        // farm onboarding form again.


        [HttpGet]
        public IActionResult Onboarding(bool forceNew = false)
        {
            // ---------------------------------------------------------
            // Logged-in user
            // ---------------------------------------------------------
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    // Explicitly requested a new farm.
                    // Do not run any existing-farm routing or TempData logic.
                    if (forceNew)
                    {
                        return View(
                            new FarmOnboardingViewModel());
                    }

                    // -------------------------------------------------
                    // Restore a farm that was completed anonymously
                    // before login/register.
                    //
                    // TempData is protected by ASP.NET Core Data Protection.
                    // If an old/corrupt TempData cookie exists, do not let it
                    // break the Create Your Farm page. We simply continue with
                    // the normal farm routing.
                    // -------------------------------------------------
                    FarmOnboardingViewModel? pendingModel = null;

                    try
                    {
                        if (TempData.TryGetValue(
                                "PendingFarmOnboarding",
                                out var pendingValue)
                            && pendingValue is string pendingJson
                            && !string.IsNullOrWhiteSpace(pendingJson))
                        {
                            pendingModel =
                                JsonSerializer.Deserialize<FarmOnboardingViewModel>(
                                    pendingJson);
                        }
                    }
                    catch
                    {
                        // Ignore invalid/old TempData and continue normally.
                        pendingModel = null;
                    }

                    if (pendingModel != null)
                    {
                        try
                        {
                            // Save the farm under the authenticated user.
                            var farm =
                                new FarmProfile
                                {
                                    UserId = userId,
                                    FarmerName = pendingModel.FarmerName,
                                    Location = pendingModel.Location,
                                    ExperienceLevel = pendingModel.ExperienceLevel,
                                    LandSize = pendingModel.LandSize,
                                    LandUnit = pendingModel.LandUnit,
                                    SoilType = pendingModel.SoilType,
                                    WaterSource = pendingModel.WaterSource,
                                    FarmingMethod = pendingModel.FarmingMethod,
                                    Budget = pendingModel.Budget,
                                    CreatedAt = DateTime.UtcNow
                                };

                            var farmId =
                                _db.SaveFarm(farm);

                            // -------------------------------------------------
                            // Save soil assessment from onboarding.
                            // -------------------------------------------------
                            var soilAssessment =
                                new SoilAssessment
                                {
                                    FarmProfileId = farmId,
                                    SoilKnowledge = pendingModel.SoilKnowledge,
                                    SoilTexture = pendingModel.SoilTexture,
                                    SoilColour = pendingModel.SoilColour,
                                    Drainage = pendingModel.Drainage,
                                    PreviousCrop = pendingModel.PreviousCrop,
                                    SoilTestStatus = pendingModel.SoilTestStatus,
                                    CreatedAt = DateTime.UtcNow
                                };

                            soilAssessment.Result =
                                BuildSoilResult(soilAssessment);

                            _db.SaveSoilAssessment(soilAssessment);

                            // -------------------------------------------------
                            // Create the initial farm journey.
                            // -------------------------------------------------
                            var activities =
                                new List<FarmActivity>
                                {
                                    new FarmActivity
                                    {
                                        FarmProfileId = farmId,
                                        Title = "Understand your soil",
                                        Description =
                                            "Learn your soil type and understand what your crop needs.",
                                        ActivityDate = DateTime.Today,
                                        Category = "Soil"
                                    },

                                    new FarmActivity
                                    {
                                        FarmProfileId = farmId,
                                        Title = "Understand your water",
                                        Description =
                                            "Review your water source and irrigation needs.",
                                        ActivityDate = DateTime.Today.AddDays(1),
                                        Category = "Water"
                                    },

                                    new FarmActivity
                                    {
                                        FarmProfileId = farmId,
                                        Title = "Learn about choosing your crops",
                                        Description =
                                            "Explore how to choose suitable crops for your land, soil, water and local conditions.",
                                        ActivityDate = DateTime.Today.AddDays(2),
                                        Category = "Crop"
                                    },

                                    new FarmActivity
                                    {
                                        FarmProfileId = farmId,
                                        Title = "Prepare your growing area",
                                        Description =
                                            "Prepare your land or growing space before planting.",
                                        ActivityDate = DateTime.Today.AddDays(4),
                                        Category = "Preparation"
                                    },

                                    new FarmActivity
                                    {
                                        FarmProfileId = farmId,
                                        Title = "Plant your crop",
                                        Description =
                                            "Add your crop and record the sowing date to begin your crop journey.",
                                        ActivityDate = DateTime.Today.AddDays(6),
                                        Category = "Planting"
                                    },

                                    new FarmActivity
                                    {
                                        FarmProfileId = farmId,
                                        Title = "Check the weather",
                                        Description =
                                            "Review weather conditions before important farm activities.",
                                        ActivityDate = DateTime.Today.AddDays(5),
                                        Category = "Weather"
                                    },

                                    new FarmActivity
                                    {
                                        FarmProfileId = farmId,
                                        Title = "Record your first crop observation",
                                        Description =
                                            "Take a photo of your crop and record what you observe.",
                                        ActivityDate = DateTime.Today.AddDays(7),
                                        Category = "Monitoring"
                                    },

                                    new FarmActivity
                                    {
                                        FarmProfileId = farmId,
                                        Title = "Monitor crop growth",
                                        Description =
                                            "Review crop growth and look for changes.",
                                        ActivityDate = DateTime.Today.AddDays(14),
                                        Category = "Monitoring"
                                    },

                                    new FarmActivity
                                    {
                                        FarmProfileId = farmId,
                                        Title = "Learn about harvest readiness",
                                        Description =
                                            "Understand the signs that your crop is approaching harvest.",
                                        ActivityDate = DateTime.Today.AddDays(30),
                                        Category = "Harvest"
                                    }
                                };

                            foreach (var activity in activities)
                            {
                                _db.AddActivity(activity);
                            }

                            // Pending data has now been converted into a farm.
                            TempData.Remove("PendingFarmOnboarding");
                            TempData.Remove("OnboardingLoginMessage");

                            return RedirectToAction(
                                nameof(MyFarms));
                        }
                        catch
                        {
                            // If the pending farm cannot be restored/saved,
                            // continue with normal existing-farm routing.
                        }
                    }

                    // -------------------------------------------------
                    // No pending farm: handle existing farms.
                    // -------------------------------------------------
                    var farms =
                        _db.GetFarmsByUserId(userId);

                    if (farms != null)
                    {
                        // One existing farm -> open its crops.
                        if (farms.Count() == 1)
                        {
                            var farm =
                                farms.First();

                            return RedirectToAction(
                                nameof(MyCrops),
                                new
                                {
                                    farmId = farm.Id
                                });
                        }

                        // Multiple existing farms -> let the user choose.
                        if (farms.Count() > 1)
                        {
                            return RedirectToAction(
                                nameof(MyFarms));
                        }
                    }

                    // No farms -> show the Create Your Farm form.
                    return View(
                        new FarmOnboardingViewModel());
                }
            }

            // ---------------------------------------------------------
            // New / anonymous user
            // ---------------------------------------------------------
            var model =
                new FarmOnboardingViewModel();

            // Restore onboarding data if an anonymous user completed the
            // form before login/register. Protect this read from an invalid
            // or stale TempData cookie as well.
            try
            {
                if (TempData.TryGetValue(
                        "PendingFarmOnboarding",
                        out var viewPendingValue)
                    && viewPendingValue is string viewPendingJson
                    && !string.IsNullOrWhiteSpace(viewPendingJson))
                {
                    var pendingModel =
                        JsonSerializer.Deserialize<FarmOnboardingViewModel>(
                            viewPendingJson);

                    if (pendingModel != null)
                    {
                        model = pendingModel;
                    }
                }
            }
            catch
            {
                // Show a fresh onboarding form if TempData cannot be read.
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Onboarding(
            FarmOnboardingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ---------------------------------------------------------
            // Anonymous user:
            // Preserve the completed form and send the user to login.
            // After login/register, the user returns to Onboarding and
            // the form is restored automatically.
            // ---------------------------------------------------------
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                TempData["PendingFarmOnboarding"] =
                    JsonSerializer.Serialize(model);

                TempData["OnboardingLoginMessage"] =
                    "Please login or register to save your farm and continue to Crop Doctor.";

                return Challenge(
                    new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                    {
                        RedirectUri = Url.Action(
                            nameof(Onboarding),
                            "Home") ?? "/Home/Onboarding"
                    });
            }

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // ---------------------------------------------------------
            // Existing logged-in farm creation logic
            // ---------------------------------------------------------
            var farm = new FarmProfile
            {
                UserId = userId,
                FarmerName = model.FarmerName,
                Location = model.Location,
                ExperienceLevel = model.ExperienceLevel,
                LandSize = model.LandSize,
                LandUnit = model.LandUnit,
                SoilType = model.SoilType,
                WaterSource = model.WaterSource,
                FarmingMethod = model.FarmingMethod,
                Budget = model.Budget,
                CreatedAt = DateTime.UtcNow
            };

            var farmId = _db.SaveFarm(farm);


            // Create initial farm journey

            var activities = new List<FarmActivity>
            {
                new FarmActivity
                {
                    FarmProfileId = farmId,
                    Title = "Understand your soil",
                    Description =
                        "Learn your soil type and understand what your crop needs.",
                    ActivityDate = DateTime.Today,
                    Category = "Soil"
                },

                new FarmActivity
                {
                    FarmProfileId = farmId,
                    Title = "Understand your water",
                    Description =
                        "Review your water source and irrigation needs.",
                    ActivityDate = DateTime.Today.AddDays(1),
                    Category = "Water"
                },

                new FarmActivity
                {
                    FarmProfileId = farmId,
                    Title = "Learn about choosing your crops",
                    Description =
                        "Explore how to choose suitable crops for your land, soil, water and local conditions.",
                    ActivityDate = DateTime.Today.AddDays(2),
                    Category = "Crop"
                },

                new FarmActivity
                {
                    FarmProfileId = farmId,
                    Title = "Prepare your growing area",
                    Description =
                        "Prepare your land or growing space before planting.",
                    ActivityDate = DateTime.Today.AddDays(4),
                    Category = "Preparation"
                },
                new FarmActivity
{
    FarmProfileId = farmId,
    Title = "Plant your crop",
    Description =
        "Add your crop and record the sowing date to begin your crop journey.",
    ActivityDate = DateTime.Today.AddDays(6),
    Category = "Planting"
},

                new FarmActivity
                {
                    FarmProfileId = farmId,
                    Title = "Check the weather",
                    Description =
                        "Review weather conditions before important farm activities.",
                    ActivityDate = DateTime.Today.AddDays(5),
                    Category = "Weather"
                },

                new FarmActivity
                {
                    FarmProfileId = farmId,
                    Title = "Record your first crop observation",
                    Description =
                        "Take a photo of your crop and record what you observe.",
                    ActivityDate = DateTime.Today.AddDays(7),
                    Category = "Monitoring"
                },

                new FarmActivity
                {
                    FarmProfileId = farmId,
                    Title = "Monitor crop growth",
                    Description =
                        "Review crop growth and look for changes.",
                    ActivityDate = DateTime.Today.AddDays(14),
                    Category = "Monitoring"
                },

                new FarmActivity
                {
                    FarmProfileId = farmId,
                    Title = "Learn about harvest readiness",
                    Description =
                        "Understand the signs that your crop is approaching harvest.",
                    ActivityDate = DateTime.Today.AddDays(30),
                    Category = "Harvest"
                }
            };


            foreach (var activity in activities)
            {
                _db.AddActivity(activity);
            }

            return RedirectToAction(
                nameof(FarmCreated),
                new { id = farmId });
        }


        // =========================================================
        // FARM CREATED
        // =========================================================

        [HttpGet]
        public IActionResult FarmCreated(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm = _db.GetFarm(id, userId);

            if (farm == null)
            {
                return NotFound();
            }

            return View(farm);
        }

        // =========================================================
        // PREPARE YOUR GROWING AREA
        // =========================================================

        [Authorize]
        [HttpGet]
        public IActionResult PrepareFarm(int farmId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            var crops =
                _db.GetFarmCrops(
                    farmId,
                    userId);

            ViewBag.CropCount =
                crops.Count;

            return View(farm);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteFarmPreparation(
            int farmId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            var activity =
                _db.GetActivities(farmId)
                    .FirstOrDefault(
                        a =>
                            a.Title ==
                            "Prepare your growing area");

            if (activity != null)
            {
                _db.UpdateActivityCompletion(
                    activity.Id,
                    farmId,
                    true);
            }

            return RedirectToAction(
                nameof(Dashboard),
                new
                {
                    id = farmId
                });
        }
        // =========================================================
        // HARVEST READINESS
        // =========================================================

        [Authorize]
        [HttpGet]
        public IActionResult HarvestReadiness(int farmId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            var crops =
                _db.GetFarmCrops(
                    farmId,
                    userId);

            ViewBag.Crops = crops;

            return View(farm);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteHarvestReadiness(
            int farmId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            var activity =
                _db.GetActivities(farmId)
                    .FirstOrDefault(
                        a =>
                            a.Title ==
                            "Learn about harvest readiness");

            if (activity != null)
            {
                _db.UpdateActivityCompletion(
                    activity.Id,
                    farmId,
                    true);
            }

            return RedirectToAction(
                nameof(Dashboard),
                new
                {
                    id = farmId
                });
        }

        // =========================================================
        // DASHBOARD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard(int id)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    id,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            // =====================================================
            // FARM JOURNEY
            // Only these 5 activities count toward progress.
            // Weather, AI and first crop observation remain useful
            // features but are NOT Farm Journey milestones.
            // =====================================================

            var allActivities =
                _db.GetActivities(id);

            var coreJourneyTitles =
           new[]
           {
        "Learn about choosing your crops",
        "Prepare your growing area",
        "Plant your crop",
        "Monitor crop growth",
        "Learn about harvest readiness"
           };


            // =====================================================
            // ADD PLANTING MILESTONE FOR OLDER FARMS
            // =====================================================

            // =====================================================
            // ADD / SYNC PLANTING MILESTONE
            // =====================================================

            var plantingActivity =
                allActivities.FirstOrDefault(
                    a =>
                        a.Title ==
                        "Plant your crop");


            // =====================================================
            // ADD PLANTING MILESTONE FOR OLDER FARMS
            // =====================================================

            if (plantingActivity == null)
            {
                _db.AddActivity(
                    new FarmActivity
                    {
                        FarmProfileId = id,

                        Title =
                            "Plant your crop",

                        Description =
                            "Add your crop and record the sowing date to begin your crop journey.",

                        ActivityDate =
                            DateTime.Today.AddDays(6),

                        Category =
                            "Planting"
                    });

                allActivities =
                    _db.GetActivities(id);

                plantingActivity =
                    allActivities.FirstOrDefault(
                        a =>
                            a.Title ==
                            "Plant your crop");
            }


            // =====================================================
            // AUTO-COMPLETE FOR ALREADY REGISTERED CROPS
            // If any crop already has a sowing date,
            // planting has already started.
            // =====================================================

            var existingCrops =
                _db.GetFarmCrops(
                    id,
                    userId);

            var cropAlreadyPlanted =
     existingCrops.Any(
         c =>
             c.SowingDate.HasValue);

            if (plantingActivity != null)
            {
                _db.UpdateActivityCompletion(
                    plantingActivity.Id,
                    id,
                    cropAlreadyPlanted);

                // Refresh activities so the dashboard
                // always reflects the current crop status.
                allActivities =
                    _db.GetActivities(id);
            }


            // =====================================================
            // ONLY CORE JOURNEY ACTIVITIES
            // =====================================================

            var activities =
                allActivities
                    .Where(
                        a =>
                            coreJourneyTitles.Contains(
                                a.Title))
                    .OrderBy(
                        a =>
                            Array.IndexOf(
                                coreJourneyTitles,
                                a.Title))
                    .ToList();


            // =====================================================
            // DASHBOARD MODEL
            // =====================================================

            var model =
                new DashboardViewModel
                {
                    Farm = farm,

                    Activities =
                        activities,

                    CompletedActivities =
                        activities.Count(
                            a => a.IsCompleted),

                    TotalActivities =
                        activities.Count
                };


            // =====================================================
            // LIVE WEATHER
            // =====================================================

            var weather =
                await _weatherService.GetWeatherAsync(
                    farm.Location);

            ViewBag.Weather =
                weather;

            return View(model);
        }

        // =========================================================
        // UPDATE FARM ACTIVITY COMPLETION
        // =========================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateActivityCompletion(
            int activityId,
            int farmId,
            bool isCompleted)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            var activity =
                _db.GetActivities(farmId)
                    .FirstOrDefault(
                        a => a.Id == activityId);

            if (activity == null)
            {
                return NotFound();
            }

            _db.UpdateActivityCompletion(
                activityId,
                farmId,
                isCompleted);

            return RedirectToAction(
                nameof(Dashboard),
                new
                {
                    id = farmId
                });
        }

        // =========================================================
        // COMPLETE INDIVIDUAL LEARNING LESSON
        // =========================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteLesson(
            int farmId,
            int topicId,
            int lessonId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            // Make sure this lesson actually belongs
            // to the selected topic.
            var lessonExists =
                GetLessons(topicId)
                    .Any(l => l.Id == lessonId);

            if (!lessonExists)
            {
                return NotFound();
            }

            _db.MarkLessonCompleted(
                farmId,
                topicId,
                lessonId);

            return RedirectToAction(
                nameof(Lesson),
                new
                {
                    id = topicId,
                    farmId = farmId
                });
        }
        // =========================================================
        // COMPLETE LEARNING ACTIVITY
        // =========================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteLearning(
            int topicId,
            int farmId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            // The Farm Journey activity represented by
            // this learning journey is Topic 4.
            if (topicId != 4)
            {
                return BadRequest();
            }

            // =====================================================
            // VERIFY ALL 11 LEARNING TOPICS
            // =====================================================

            for (int learningTopicId = 1;
                 learningTopicId <= 11;
                 learningTopicId++)
            {
                var lessons =
                    GetLessons(learningTopicId);

                var completedLessonIds =
                    _db.GetCompletedLessonIds(
                        farm.Id,
                        learningTopicId);

                // A topic cannot be considered complete
                // if it has no lessons or even one lesson
                // is still incomplete.
                if (lessons.Count == 0)
                {
                    return RedirectToAction(
                        nameof(Teach),
                        new
                        {
                            farmId = farm.Id
                        });
                }

                if (lessons.Any(
                    lesson =>
                        !completedLessonIds.Contains(
                            lesson.Id)))
                {
                    return RedirectToAction(
                        nameof(Teach),
                        new
                        {
                            farmId = farm.Id
                        });
                }
            }

            // =====================================================
            // ALL 11 TOPICS ARE COMPLETE
            // =====================================================

            var activity =
                _db.GetActivities(farmId)
                    .FirstOrDefault(
                        a =>
                            a.Title ==
                            "Learn about choosing your crops");

            if (activity == null)
            {
                return NotFound();
            }

            _db.UpdateActivityCompletion(
                activity.Id,
                farmId,
                true);

            return RedirectToAction(
                nameof(Dashboard),
                new
                {
                    id = farmId
                });
        }

        // =========================================================
        // TEACH / LEARNING
        // =========================================================

        [HttpGet]
        public IActionResult Teach(int? farmId)
        {
            FarmProfile? farm = null;

            if (farmId.HasValue)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    farm = _db.GetFarm(farmId.Value, userId);
                }
            }

            var topics = new List<LearningTopic>
            {
                new LearningTopic
                {
                    Id = 1,
                    Title = "Farming Basics",
                    Description =
                        "Understand the fundamentals of farming before you begin.",
                    Icon = "🌱",
                    Category = "Getting Started",
                    Order = 1,
                    LessonCount = 5
                },

                new LearningTopic
                {
                    Id = 2,
                    Title = "Understand Your Soil",
                    Description =
                        "Learn soil types, soil testing, pH and soil health.",
                    Icon = "🌍",
                    Category = "Farm Essentials",
                    Order = 2,
                    LessonCount = 6
                },

                new LearningTopic
                {
                    Id = 3,
                    Title = "Understand Water",
                    Description =
                        "Learn about water sources, quality and irrigation.",
                    Icon = "💧",
                    Category = "Farm Essentials",
                    Order = 3,
                    LessonCount = 5
                },

                new LearningTopic
                {
                    Id = 4,
                    Title = "Choose Your Crop",
                    Description =
                        "Understand how to select crops for your farm and conditions.",
                    Icon = "🌾",
                    Category = "Crops",
                    Order = 4,
                    LessonCount = 6
                },

                new LearningTopic
                {
                    Id = 5,
                    Title = "Seeds & Planting",
                    Description =
                        "Learn seed selection, preparation, planting and spacing.",
                    Icon = "🌱",
                    Category = "Crops",
                    Order = 5,
                    LessonCount = 6
                },

                new LearningTopic
                {
                    Id = 6,
                    Title = "Crop Growth",
                    Description =
                        "Understand the stages of crop growth and plant care.",
                    Icon = "🌿",
                    Category = "Crop Care",
                    Order = 6,
                    LessonCount = 7
                },

                new LearningTopic
                {
                    Id = 7,
                    Title = "Pests & Diseases",
                    Description =
                        "Learn how to observe crop problems and understand possible causes.",
                    Icon = "🐛",
                    Category = "Crop Care",
                    Order = 7,
                    LessonCount = 7
                },

                new LearningTopic
                {
                    Id = 8,
                    Title = "Weather & Farming",
                    Description =
                        "Understand how weather affects farming decisions.",
                    Icon = "🌦️",
                    Category = "Farm Essentials",
                    Order = 8,
                    LessonCount = 5
                },

                new LearningTopic
                {
                    Id = 9,
                    Title = "Irrigation",
                    Description =
                        "Learn practical irrigation and water management concepts.",
                    Icon = "💦",
                    Category = "Farm Essentials",
                    Order = 9,
                    LessonCount = 5
                },

                new LearningTopic
                {
                    Id = 10,
                    Title = "Harvest",
                    Description =
                        "Learn how to recognize harvest readiness and prepare for harvest.",
                    Icon = "🌾",
                    Category = "Harvest",
                    Order = 10,
                    LessonCount = 5
                },

                new LearningTopic
                {
                    Id = 11,
                    Title = "Selling Your Produce",
                    Description =
                        "Learn the basics of preparing, storing and selling farm produce.",
                    Icon = "💰",
                    Category = "After Harvest",
                    Order = 11,
                    LessonCount = 5
                }
            };

            // =====================================================
            // LEARNING PROGRESS
            // =====================================================

            var completedTopicIds =
                new HashSet<int>();

            if (farm != null)
            {
                foreach (var learningTopic in topics)
                {
                    var lessons =
                        GetLessons(learningTopic.Id);

                    var completedLessonIds =
                        _db.GetCompletedLessonIds(
                            farm.Id,
                            learningTopic.Id);

                    if (lessons.Count > 0 &&
                        lessons.All(
                            lesson =>
                                completedLessonIds.Contains(
                                    lesson.Id)))
                    {
                        completedTopicIds.Add(
                            learningTopic.Id);
                    }
                }
            }

            ViewData["CompletedTopicIds"] =
                completedTopicIds;

            ViewData["AllLearningCompleted"] =
                farm != null &&
                topics.Count == 11 &&
                completedTopicIds.Count == 11;


            ViewData["FarmId"] =
                farm?.Id ?? 0;

            ViewData["FarmerName"] =
                farm?.FarmerName ?? "";

            ViewData["Crop"] =
                farm?.Crop ?? "";

            return View(topics);
        }


        [HttpGet]
        public IActionResult Lesson(
            int id,
            int? farmId)
        {
            var topics = new List<LearningTopic>
            {
                new LearningTopic
                {
                    Id = 1,
                    Title = "Farming Basics",
                    Description =
                        "Understand the fundamentals of farming.",
                    Icon = "🌱",
                    Category = "Getting Started"
                },

                new LearningTopic
                {
                    Id = 2,
                    Title = "Understand Your Soil",
                    Description =
                        "Learn soil types, testing, pH and soil health.",
                    Icon = "🌍",
                    Category = "Farm Essentials"
                },

                new LearningTopic
                {
                    Id = 3,
                    Title = "Understand Water",
                    Description =
                        "Learn water sources, quality and irrigation.",
                    Icon = "💧",
                    Category = "Farm Essentials"
                },

                new LearningTopic
                {
                    Id = 4,
                    Title = "Choose Your Crop",
                    Description =
                        "Understand crop selection.",
                    Icon = "🌾",
                    Category = "Crops"
                },

                new LearningTopic
                {
                    Id = 5,
                    Title = "Seeds & Planting",
                    Description =
                        "Learn seed selection and planting.",
                    Icon = "🌱",
                    Category = "Crops"
                },

                new LearningTopic
                {
                    Id = 6,
                    Title = "Crop Growth",
                    Description =
                        "Understand crop growth stages.",
                    Icon = "🌿",
                    Category = "Crop Care"
                },

                new LearningTopic
                {
                    Id = 7,
                    Title = "Pests & Diseases",
                    Description =
                        "Learn how to observe crop problems.",
                    Icon = "🐛",
                    Category = "Crop Care"
                },

                new LearningTopic
                {
                    Id = 8,
                    Title = "Weather & Farming",
                    Description =
                        "Understand weather and farming decisions.",
                    Icon = "🌦️",
                    Category = "Farm Essentials"
                },

                new LearningTopic
                {
                    Id = 9,
                    Title = "Irrigation",
                    Description =
                        "Learn irrigation and water management.",
                    Icon = "💦",
                    Category = "Farm Essentials"
                },

                new LearningTopic
                {
                    Id = 10,
                    Title = "Harvest",
                    Description =
                        "Learn how to recognize harvest readiness.",
                    Icon = "🌾",
                    Category = "Harvest"
                },

                new LearningTopic
                {
                    Id = 11,
                    Title = "Selling Your Produce",
                    Description =
                        "Learn the basics of selling farm produce.",
                    Icon = "💰",
                    Category = "After Harvest"
                }
            };


            var topic =
                topics.FirstOrDefault(t => t.Id == id);

            if (topic == null)
            {
                return NotFound();
            }
            var lessons = GetLessons(id);

            FarmProfile? farm = null;

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (farmId.HasValue &&
                !string.IsNullOrEmpty(userId))
            {
                farm = _db.GetFarm(
                    farmId.Value,
                    userId);
            }

            ViewData["FarmId"] =
                farm?.Id ?? 0;

            ViewData["Crop"] =
                farm?.Crop ?? "";

            ViewData["FarmerName"] =
                farm?.FarmerName ?? "";


            // =====================================================
            // LESSON COMPLETION PROGRESS
            // =====================================================

            var completedLessonIds =
                farm != null
                    ? _db.GetCompletedLessonIds(
                        farm.Id,
                        id)
                    : new List<int>();

            var topicCompleted =
                lessons.Count > 0 &&
                lessons.All(
                    lesson =>
                        completedLessonIds.Contains(
                            lesson.Id));

            ViewData["CompletedLessonIds"] =
                completedLessonIds;

            ViewData["TopicCompleted"] =
                topicCompleted;


            var model = new LessonPageViewModel
            {
                Topic = topic,
                Lessons = lessons
            };

            return View(model);
        }


        private List<LearningLesson> GetLessons(
            int topicId)
        {
            var lessons = new List<LearningLesson>();

            switch (topicId)
            {
                case 1:

                    lessons.Add(new LearningLesson
                    {
                        Id = 101,
                        TopicId = 1,
                        Title = "What is farming?",
                        Introduction =
                            "Farming is the practice of growing crops and managing natural resources to produce food and other useful products.",
                        Content =
                            "Farming involves several connected activities: understanding your land, choosing suitable crops, preparing the growing area, planting, managing water and nutrients, monitoring crop health, harvesting and handling the produce.",
                        KeyPoint =
                            "Good farming starts with understanding your farm before planting.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 102,
                        TopicId = 1,
                        Title = "The farming cycle",
                        Introduction =
                            "A crop goes through a series of stages from planning to harvest.",
                        Content =
                            "A typical farming cycle includes planning, soil and water assessment, crop selection, land preparation, seed selection, planting, crop establishment, growth and care, flowering or fruiting, harvest and post-harvest handling.",
                        KeyPoint =
                            "Every stage affects the stages that come after it.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 103,
                        TopicId = 1,
                        Title = "What should a beginner learn first?",
                        Introduction =
                            "A beginner does not need to learn everything at once.",
                        Content =
                            "Start with your location, available growing space, soil, water source and the crop you want to grow. Then learn the crop's growing requirements and local conditions. Keeping simple records of planting dates, weather and observations can make future decisions easier.",
                        KeyPoint =
                            "Start small, observe carefully and keep records.",
                        Order = 3
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 104,
                        TopicId = 1,
                        Title = "Why observation matters",
                        Introduction =
                            "Farmers make many decisions by observing changes in their crops and environment.",
                        Content =
                            "Regularly observe plant growth, leaf colour, moisture, pests, disease symptoms, flowering and fruit development. Compare changes over time instead of making decisions from a single observation.",
                        KeyPoint =
                            "Regular observation helps you notice problems early.",
                        Order = 4
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 105,
                        TopicId = 1,
                        Title = "Start with a farm plan",
                        Introduction =
                            "A simple plan helps you organize your first crop.",
                        Content =
                            "Record your location, growing area, soil information, water source, crop, expected planting period and available budget. Your plan can change as you learn more.",
                        KeyPoint =
                            "A farm plan is a starting point, not a fixed rule.",
                        Order = 5
                    });

                    break;


                case 2:

                    lessons.Add(new LearningLesson
                    {
                        Id = 201,
                        TopicId = 2,
                        Title = "What is soil?",
                        Introduction =
                            "Soil is the growing medium that supports plant roots and supplies water, air and nutrients.",
                        Content =
                            "Soil contains mineral particles, organic matter, water, air and living organisms. Different soils behave differently depending on their texture, structure, organic matter and drainage.",
                        KeyPoint =
                            "Healthy soil supports healthy roots.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 202,
                        TopicId = 2,
                        Title = "Common soil types",
                        Introduction =
                            "Different soils have different characteristics.",
                        Content =
                            "Sandy soils generally drain quickly. Clay soils can hold more water but may drain slowly. Loamy soils contain a mixture of particle sizes and are often suitable for many crops when well managed. Local soil characteristics can vary considerably.",
                        KeyPoint =
                            "Soil type affects water retention, drainage and crop management.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 203,
                        TopicId = 2,
                        Title = "Why soil testing matters",
                        Introduction =
                            "A soil test can provide information that cannot reliably be determined just by looking at soil.",
                        Content =
                            "Depending on the test, soil analysis can provide measurements such as pH and available nutrients. Results should be interpreted for the specific crop and local conditions.",
                        KeyPoint =
                            "Use soil-test results rather than guessing nutrient requirements.",
                        Order = 3
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 204,
                        TopicId = 2,
                        Title = "Understanding soil pH",
                        Introduction =
                            "Soil pH describes how acidic or alkaline the soil is.",
                        Content =
                            "Different crops have different preferred pH ranges. Very acidic or alkaline conditions can affect nutrient availability. A soil test is the appropriate way to determine pH accurately.",
                        KeyPoint =
                            "Know your soil pH before making major soil amendments.",
                        Order = 4
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 205,
                        TopicId = 2,
                        Title = "Improving soil health",
                        Introduction =
                            "Soil health is about maintaining a productive and biologically active growing environment.",
                        Content =
                            "Practices can include maintaining organic matter, reducing unnecessary soil disturbance, managing erosion, maintaining suitable drainage and using crop rotations or other locally appropriate practices.",
                        KeyPoint =
                            "Soil improvement is usually a long-term process.",
                        Order = 5
                    });

                    break;


                case 3:

                    lessons.Add(new LearningLesson
                    {
                        Id = 301,
                        TopicId = 3,
                        Title = "Why plants need water",
                        Introduction =
                            "Water is essential for plant growth and many biological processes.",
                        Content =
                            "Plants use water for photosynthesis, nutrient transport, cell expansion and temperature regulation. The required amount depends on crop, growth stage, weather and soil conditions.",
                        KeyPoint =
                            "Water needs change throughout the crop cycle.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 302,
                        TopicId = 3,
                        Title = "Understanding your water source",
                        Introduction =
                            "The source and quality of irrigation water matter.",
                        Content =
                            "Common sources include rainfall, wells, borewells, canals and supplied water. Water quality can affect soil and crop performance, so testing may be appropriate when quality is uncertain.",
                        KeyPoint =
                            "Know both the quantity and quality of your available water.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 303,
                        TopicId = 3,
                        Title = "Overwatering and underwatering",
                        Introduction =
                            "Both too little and too much water can harm crops.",
                        Content =
                            "Water stress can cause wilting and reduced growth, while excessive moisture can reduce root-zone oxygen and contribute to some root problems. Irrigation should consider soil moisture, crop stage and weather.",
                        KeyPoint =
                            "More water is not always better.",
                        Order = 3
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 304,
                        TopicId = 3,
                        Title = "Water quality",
                        Introduction =
                            "Irrigation water contains dissolved substances that may affect crops and soil.",
                        Content =
                            "Depending on the source, testing can include measures such as salinity and other water-quality parameters. Local agricultural laboratories can help interpret results.",
                        KeyPoint =
                            "Test uncertain water sources before relying on them for intensive cultivation.",
                        Order = 4
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 305,
                        TopicId = 3,
                        Title = "Efficient water use",
                        Introduction =
                            "Efficient irrigation aims to supply plants with appropriate water while reducing unnecessary losses.",
                        Content =
                            "Methods such as drip irrigation can deliver water closer to plant roots. Mulching and irrigation scheduling can also help reduce evaporation in suitable systems.",
                        KeyPoint =
                            "Efficient irrigation combines the right method with the right timing.",
                        Order = 5
                    });

                    break;


                case 4:

                    lessons.Add(new LearningLesson
                    {
                        Id = 401,
                        TopicId = 4,
                        Title = "How to choose a crop",
                        Introduction =
                            "Crop selection should consider your local conditions and resources.",
                        Content =
                            "Consider climate, season, soil, water availability, growing space, crop duration, labour, budget, local demand and your experience.",
                        KeyPoint =
                            "Choose crops that fit your resources and local growing conditions.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 402,
                        TopicId = 4,
                        Title = "Season matters",
                        Introduction =
                            "Many crops perform best when planted during suitable seasons.",
                        Content =
                            "Temperature, rainfall, day length and humidity can influence crop establishment and development. Always check crop-specific and locally relevant planting guidance.",
                        KeyPoint =
                            "The right crop at the wrong time can still perform poorly.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 403,
                        TopicId = 4,
                        Title = "Crop duration",
                        Introduction =
                            "Different crops occupy your growing space for different lengths of time.",
                        Content =
                            "Short-duration crops may allow more crop cycles, while longer-duration crops require longer commitments of land, water and labour. Use the expected duration when planning your farm calendar.",
                        KeyPoint =
                            "Crop duration affects your entire farm schedule.",
                        Order = 3
                    });

                    break;


                case 5:

                    lessons.Add(new LearningLesson
                    {
                        Id = 501,
                        TopicId = 5,
                        Title = "Choosing good seed",
                        Introduction =
                            "Seed quality can strongly influence crop establishment.",
                        Content =
                            "Use seed from reliable sources and select varieties suitable for your region and intended purpose. Check the seed label and recommended storage conditions.",
                        KeyPoint =
                            "Start with suitable, quality seed from a reliable source.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 502,
                        TopicId = 5,
                        Title = "Seed preparation",
                        Introduction =
                            "Some crops require specific seed preparation before planting.",
                        Content =
                            "Depending on the crop, preparation may include cleaning, grading, treatment or soaking. Follow crop-specific and locally recommended instructions rather than using a universal treatment.",
                        KeyPoint =
                            "Seed preparation is crop-specific.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 503,
                        TopicId = 5,
                        Title = "Planting depth and spacing",
                        Introduction =
                            "Plant spacing affects competition between plants.",
                        Content =
                            "Recommended depth and spacing depend on crop, variety, soil and production system. Follow reliable crop-specific recommendations.",
                        KeyPoint =
                            "Correct spacing gives plants room to develop.",
                        Order = 3
                    });

                    break;


                case 6:

                    lessons.Add(new LearningLesson
                    {
                        Id = 601,
                        TopicId = 6,
                        Title = "Understanding crop growth stages",
                        Introduction =
                            "Crops pass through recognizable development stages.",
                        Content =
                            "Depending on the crop, stages may include germination, establishment, vegetative growth, flowering, fruit or seed development and maturity. Management needs can change at each stage.",
                        KeyPoint =
                            "Crop care should change as the plant develops.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 602,
                        TopicId = 6,
                        Title = "Monitoring plant growth",
                        Introduction =
                            "Regular observation helps you understand whether plants are developing normally.",
                        Content =
                            "Observe new leaves, plant height, colour, branching, flowering and other crop-specific indicators. Record changes over time.",
                        KeyPoint =
                            "Compare observations over time instead of relying on one snapshot.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 603,
                        TopicId = 6,
                        Title = "Plant nutrition basics",
                        Introduction =
                            "Plants require several essential nutrients.",
                        Content =
                            "Nutrients such as nitrogen, phosphorus and potassium have important roles, along with secondary and micronutrients. Deficiency symptoms can overlap with other problems, so testing and expert guidance may be needed before treatment.",
                        KeyPoint =
                            "Do not assume a nutrient deficiency from appearance alone.",
                        Order = 3
                    });

                    break;


                case 7:

                    lessons.Add(new LearningLesson
                    {
                        Id = 701,
                        TopicId = 7,
                        Title = "Start with observation",
                        Introduction =
                            "Not every unusual leaf or plant automatically means disease.",
                        Content =
                            "Observe which plant parts are affected, how symptoms are distributed, when they started and whether environmental conditions changed. Take clear photos and keep notes.",
                        KeyPoint =
                            "Good observation comes before treatment.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 702,
                        TopicId = 7,
                        Title = "Pests and beneficial insects",
                        Introduction =
                            "Not every insect found on a crop is harmful.",
                        Content =
                            "Some insects feed on plants while others may prey on pests or support pollination. Identify the organism and assess the level of damage before deciding on control measures.",
                        KeyPoint =
                            "Identify the problem before attempting control.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 703,
                        TopicId = 7,
                        Title = "When to seek expert help",
                        Introduction =
                            "Some crop problems need professional identification.",
                        Content =
                            "If symptoms spread rapidly, affect a large area, involve valuable crops or remain unclear after observation, consult a local agricultural expert or plant diagnostic service.",
                        KeyPoint =
                            "Uncertain diagnosis is a reason to seek local expertise.",
                        Order = 3
                    });

                    break;


                case 8:

                    lessons.Add(new LearningLesson
                    {
                        Id = 801,
                        TopicId = 8,
                        Title = "Why weather matters",
                        Introduction =
                            "Weather affects nearly every stage of crop production.",
                        Content =
                            "Temperature, rainfall, humidity, wind and sunlight can affect germination, growth, flowering, disease pressure, irrigation and harvest operations.",
                        KeyPoint =
                            "Weather information helps you plan farm activities.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 802,
                        TopicId = 8,
                        Title = "Weather before farm activities",
                        Introduction =
                            "Check upcoming weather before activities that depend on conditions.",
                        Content =
                            "Examples include irrigation, spraying according to approved crop guidance, transplanting, harvesting and drying. Weather forecasts are useful but can change, so check them close to the activity.",
                        KeyPoint =
                            "Use current forecasts together with field conditions.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 803,
                        TopicId = 8,
                        Title = "Extreme weather awareness",
                        Introduction =
                            "Extreme heat, heavy rain, strong wind or cold can affect crops.",
                        Content =
                            "Watch local alerts and crop-specific recommendations during extreme weather. Protect crops and workers according to local agricultural and safety guidance.",
                        KeyPoint =
                            "Weather risk management starts with early awareness.",
                        Order = 3
                    });

                    break;


                case 9:

                    lessons.Add(new LearningLesson
                    {
                        Id = 901,
                        TopicId = 9,
                        Title = "What is irrigation?",
                        Introduction =
                            "Irrigation is the controlled supply of water to crops.",
                        Content =
                            "Irrigation supplements rainfall when natural precipitation does not meet crop requirements. The appropriate method depends on crop, soil, terrain, water availability and farm scale.",
                        KeyPoint =
                            "Irrigation should match crop and field conditions.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 902,
                        TopicId = 9,
                        Title = "Drip irrigation",
                        Introduction =
                            "Drip systems deliver water near plant roots.",
                        Content =
                            "Drip irrigation can improve water-use efficiency in suitable crops and systems. Good design, filtration, maintenance and appropriate scheduling are important.",
                        KeyPoint =
                            "Efficient equipment still needs proper management.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 903,
                        TopicId = 9,
                        Title = "Irrigation scheduling",
                        Introduction =
                            "Irrigation timing should respond to crop and field conditions.",
                        Content =
                            "Consider crop growth stage, soil moisture, weather and the irrigation system. Avoid following a fixed schedule without considering actual conditions.",
                        KeyPoint =
                            "Schedule irrigation using observations and crop requirements.",
                        Order = 3
                    });

                    break;


                case 10:

                    lessons.Add(new LearningLesson
                    {
                        Id = 1001,
                        TopicId = 10,
                        Title = "What is harvest readiness?",
                        Introduction =
                            "Harvest timing depends on the crop and the intended use.",
                        Content =
                            "Signs can include changes in colour, size, moisture, seed maturity, fruit firmness or other crop-specific indicators. Use reliable crop-specific guidance.",
                        KeyPoint =
                            "Harvest indicators are crop-specific.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 1002,
                        TopicId = 10,
                        Title = "Preparing for harvest",
                        Introduction =
                            "Harvest planning reduces avoidable losses.",
                        Content =
                            "Prepare labour, tools, containers, transport and a suitable place for handling the produce. Check weather when harvest depends on dry conditions.",
                        KeyPoint =
                            "Prepare the harvest operation before the crop is ready.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 1003,
                        TopicId = 10,
                        Title = "Post-harvest handling",
                        Introduction =
                            "Handling after harvest can affect quality and losses.",
                        Content =
                            "Keep produce clean and handle it carefully. Storage conditions vary greatly by crop, so use crop-specific recommendations for temperature, humidity and storage duration.",
                        KeyPoint =
                            "Harvest is not the end of crop management.",
                        Order = 3
                    });

                    break;


                case 11:

                    lessons.Add(new LearningLesson
                    {
                        Id = 1101,
                        TopicId = 11,
                        Title = "Know your produce",
                        Introduction =
                            "Before selling, understand what you have produced.",
                        Content =
                            "Record crop, quantity, quality, harvest date and available packaging. Accurate records help you communicate with buyers.",
                        KeyPoint =
                            "Good records make selling easier.",
                        Order = 1
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 1102,
                        TopicId = 11,
                        Title = "Finding buyers",
                        Introduction =
                            "Farm produce can reach buyers through different channels.",
                        Content =
                            "Depending on the crop and location, channels can include local markets, wholesalers, retailers, cooperatives, processors and direct customers. Compare requirements and costs.",
                        KeyPoint =
                            "Different buyers have different requirements.",
                        Order = 2
                    });

                    lessons.Add(new LearningLesson
                    {
                        Id = 1103,
                        TopicId = 11,
                        Title = "Calculate your costs",
                        Introduction =
                            "Understanding costs helps you evaluate your farming results.",
                        Content =
                            "Keep records of seed, inputs, labour, irrigation, equipment, transport and other expenses. Compare total costs with actual sales revenue.",
                        KeyPoint =
                            "Record costs throughout the crop cycle, not only at harvest.",
                        Order = 3
                    });

                    break;
            }

            return lessons
                .OrderBy(x => x.Order)
                .ToList();
        }


        // =========================================================
        // WATER ASSESSMENT
        // =========================================================

        [HttpGet]
        public IActionResult WaterAssessment(
            int? farmId,
            int? cropId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            FarmProfile? farm = null;

            if (farmId.HasValue)
            {
                farm =
                    _db.GetFarm(
                        farmId.Value,
                        userId);
            }

            if (farm == null)
            {
                return NotFound();
            }

            FarmCrop? selectedCrop = null;

            if (cropId.HasValue)
            {
                selectedCrop =
                    _db.GetFarmCrops(
                        farm.Id,
                        userId)
                    .FirstOrDefault(
                        c => c.Id == cropId.Value);

                if (selectedCrop == null)
                {
                    return NotFound();
                }

                farm.Crop =
                    selectedCrop.CropName;

                TempData["WaterCropId"] =
                    selectedCrop.Id;
            }

            var existingAssessment =
                _db.GetWaterAssessment(
                    farm.Id);

            var crop =
                _cropKnowledge.GetCrop(
                    selectedCrop?.CropName
                    ?? farm.Crop);

            var model =
                new WaterAssessmentViewModel
                {
                    Farm = farm,

                    Assessment =
                        existingAssessment
                        ?? new WaterAssessment
                        {
                            FarmProfileId =
                                farm.Id
                        },

                    HasExistingAssessment =
                        existingAssessment != null,

                    CropKnowledge = crop
                };

            ViewBag.CropId =
                selectedCrop?.Id;

            ViewBag.CropName =
                selectedCrop?.CropName
                ?? farm.Crop;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult WaterAssessment(
            WaterAssessmentViewModel model)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    model.Farm.Id,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            FarmCrop? selectedCrop = null;

            var savedCropIdValue =
                TempData.Peek("WaterCropId");

            int? savedCropId =
                savedCropIdValue is int id
                    ? id
                    : null;

            if (savedCropId.HasValue)
            {
                selectedCrop =
                    _db.GetFarmCrops(
                        farm.Id,
                        userId)
                    .FirstOrDefault(
                        c => c.Id == savedCropId.Value);
            }

            if (selectedCrop != null)
            {
                farm.Crop =
                    selectedCrop.CropName;
            }

            model.Farm = farm;

            if (string.IsNullOrWhiteSpace(
                    model.Assessment.WaterSource))
            {
                ModelState.AddModelError(
                    "Assessment.WaterSource",
                    "Please select your water source.");
            }

            if (string.IsNullOrWhiteSpace(
                    model.Assessment.WaterAvailability))
            {
                ModelState.AddModelError(
                    "Assessment.WaterAvailability",
                    "Please select your water availability.");
            }

            if (string.IsNullOrWhiteSpace(
                    model.Assessment.IrrigationMethod))
            {
                ModelState.AddModelError(
                    "Assessment.IrrigationMethod",
                    "Please select your irrigation method.");
            }

            if (!ModelState.IsValid)
            {
                model.CropKnowledge =
                    _cropKnowledge.GetCrop(
                        selectedCrop?.CropName
                        ?? farm.Crop);

                ViewBag.CropId =
                    selectedCrop?.Id;

                ViewBag.CropName =
                    selectedCrop?.CropName
                    ?? farm.Crop;

                return View(model);
            }

            model.Assessment.FarmProfileId =
                farm.Id;

            model.Assessment.CreatedAt =
                DateTime.UtcNow;

            model.Assessment.Result =
                GenerateWaterAssessmentResult(
                    farm,
                    model.Assessment);

            _db.SaveWaterAssessment(
                model.Assessment);


            // =========================================================
            // COMPLETE THE "UNDERSTAND YOUR WATER" FARM JOURNEY STEP
            // =========================================================

            var waterActivity =
                _db.GetActivities(farm.Id)
                    .FirstOrDefault(
                        a => a.Title == "Understand your water");

            if (waterActivity != null)
            {
                _db.UpdateActivityCompletion(
                    waterActivity.Id,
                    farm.Id,
                    true);
            }


            return RedirectToAction(
                nameof(WaterResult),
                new
                {
                    farmId = farm.Id,
                    cropId = selectedCrop?.Id
                });
        }

        [HttpGet]
        public IActionResult WaterResult(
            int farmId,
            int? cropId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            FarmCrop? selectedCrop = null;

            if (cropId.HasValue)
            {
                selectedCrop =
                    _db.GetFarmCrops(
                        farm.Id,
                        userId)
                    .FirstOrDefault(
                        c => c.Id == cropId.Value);

                if (selectedCrop == null)
                {
                    return NotFound();
                }

                farm.Crop =
                    selectedCrop.CropName;
            }

            var assessment =
                _db.GetWaterAssessment(
                    farmId);

            if (assessment == null)
            {
                return RedirectToAction(
                    nameof(WaterAssessment),
                    new
                    {
                        farmId,
                        cropId = selectedCrop?.Id
                    });
            }

            var crop =
                _cropKnowledge.GetCrop(
                    selectedCrop?.CropName
                    ?? farm.Crop);

            var model =
                new WaterAssessmentViewModel
                {
                    Farm = farm,

                    Assessment = assessment,

                    HasExistingAssessment = true,

                    CropKnowledge = crop
                };

            ViewBag.CropId =
                selectedCrop?.Id;

            ViewBag.CropName =
                selectedCrop?.CropName
                ?? farm.Crop;

            return View(model);
        }

        private string GenerateWaterAssessmentResult(
            FarmProfile farm,
            WaterAssessment assessment)
        {
            var result = new List<string>();

            result.Add(
                $"Your water assessment for {farm.Crop} has been recorded.");

            result.Add(
                $"Water source: {assessment.WaterSource}.");

            result.Add(
                $"Water availability: {assessment.WaterAvailability}.");

            result.Add(
                $"Irrigation method: {assessment.IrrigationMethod}.");

            if (!string.IsNullOrWhiteSpace(
                    assessment.IrrigationFrequency))
            {
                result.Add(
                    $"Irrigation frequency: {assessment.IrrigationFrequency}.");
            }

            if (assessment.DrainageCondition
                .Equals(
                    "Poor",
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Add(
                    "Your drainage is marked as poor. " +
                    "Watch for standing water around the crop " +
                    "and improve drainage where practical.");
            }
            else if (assessment.DrainageCondition
                     .Equals(
                         "Good",
                         StringComparison.OrdinalIgnoreCase))
            {
                result.Add(
                    "Your drainage is marked as good. " +
                    "Continue monitoring the field after heavy rainfall " +
                    "or irrigation.");
            }
            else
            {
                result.Add(
                    "Continue observing the field after irrigation " +
                    "and rainfall to make sure excess water does not remain " +
                    "around the crop.");
            }

            if (assessment.WaterAvailability
                .Equals(
                    "Limited",
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Add(
                    "Because water availability is limited, " +
                    "plan irrigation carefully and monitor the crop " +
                    "for signs of water stress.");
            }

            if (!string.IsNullOrWhiteSpace(
                    assessment.RecentWaterProblem)
                &&
                !assessment.RecentWaterProblem
                    .Equals(
                        "No problem",
                        StringComparison.OrdinalIgnoreCase))
            {
                result.Add(
                    $"Reported water concern: " +
                    $"{assessment.RecentWaterProblem}.");
            }

            result.Add(
                "For crop-specific irrigation decisions, " +
                "consider the crop growth stage, recent rainfall, " +
                "soil condition and reliable local agricultural guidance.");

            return string.Join(
                Environment.NewLine + Environment.NewLine,
                result);
        }


        // =========================================================
        // SOIL ASSESSMENT
        // =========================================================
        [HttpGet]
        public IActionResult SoilAssessment(
            int? farmId,
            int? cropId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            FarmProfile? farm = null;

            if (farmId.HasValue)
            {
                farm =
                    _db.GetFarm(
                        farmId.Value,
                        userId);
            }

            if (farm == null)
            {
                return RedirectToAction(
                    nameof(Onboarding));
            }

            FarmCrop? selectedCrop = null;

            if (cropId.HasValue)
            {
                selectedCrop =
                    _db.GetFarmCrops(
                        farm.Id,
                        userId)
                    .FirstOrDefault(
                        c => c.Id == cropId.Value);

                if (selectedCrop == null)
                {
                    return NotFound();
                }
            }

            var existingAssessment =
                _db.GetSoilAssessment(
                    farm.Id);

            var model =
                new SoilAssessmentViewModel
                {
                    Farm = farm,

                    Assessment =
                        existingAssessment
                        ?? new SoilAssessment
                        {
                            FarmProfileId =
                                farm.Id
                        },

                    HasExistingAssessment =
                        existingAssessment != null
                };

            if (selectedCrop != null)
            {
                farm.Crop =
                    selectedCrop.CropName;

                TempData["SoilCropId"] =
                    selectedCrop.Id;
            }
            else
            {
                TempData.Remove("SoilCropId");
            }

            ViewBag.CropId =
                selectedCrop?.Id;

            ViewBag.CropName =
                selectedCrop?.CropName
                ?? farm.Crop;

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilAssessment(
            SoilAssessmentViewModel model)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    model.Assessment.FarmProfileId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            FarmCrop? selectedCrop = null;

            var storedCropId =
                TempData.Peek("SoilCropId");

            if (storedCropId != null &&
                int.TryParse(
                    storedCropId.ToString(),
                    out var parsedCropId))
            {
                selectedCrop =
                    _db.GetFarmCrops(
                        farm.Id,
                        userId)
                    .FirstOrDefault(
                        c => c.Id == parsedCropId);

                if (selectedCrop == null)
                {
                    return NotFound();
                }

                farm.Crop =
                    selectedCrop.CropName;
            }

            model.Farm = farm;

            var assessment =
                model.Assessment;

            assessment.FarmProfileId =
                farm.Id;

            assessment.CreatedAt =
                DateTime.UtcNow;

            assessment.Result =
                BuildSoilResult(assessment);
            _db.SaveSoilAssessment(
                assessment);


            // =========================================================
            // COMPLETE THE "UNDERSTAND YOUR SOIL" FARM JOURNEY STEP
            // =========================================================

            var soilActivity =
                _db.GetActivities(farm.Id)
                    .FirstOrDefault(
                        a => a.Title == "Understand your soil");

            if (soilActivity != null)
            {
                _db.UpdateActivityCompletion(
                    soilActivity.Id,
                    farm.Id,
                    true);
            }


            return RedirectToAction(
                nameof(SoilResult),
                new { farmId = farm.Id });
        }


        private string BuildSoilResult(
            SoilAssessment assessment)
        {
            var result =
                new List<string>();

            if (assessment.SoilKnowledge == "No")
            {
                result.Add(
                    "You currently have limited information about your soil. A basic soil test is a useful next step.");
            }
            else
            {
                result.Add(
                    "You already have some knowledge about your soil. Recording your observations and test results will help build a better farm profile.");
            }

            if (assessment.SoilTexture == "Sandy")
            {
                result.Add(
                    "Sandy soil generally allows water to drain quickly, so moisture management can be important.");
            }
            else if (assessment.SoilTexture == "Clay")
            {
                result.Add(
                    "Clay soil can retain more water and may drain more slowly. Avoid making management decisions from texture alone.");
            }
            else if (assessment.SoilTexture == "Loamy")
            {
                result.Add(
                    "Loamy soil contains a mixture of particle sizes and can provide a useful growing medium when well managed.");
            }

            if (assessment.Drainage == "Quickly")
            {
                result.Add(
                    "Your observation suggests relatively quick drainage. Monitor soil moisture during dry periods.");
            }
            else if (assessment.Drainage == "Slowly")
            {
                result.Add(
                    "Your observation suggests slower drainage. Pay attention to prolonged waterlogging around the root zone.");
            }

            if (assessment.SoilTestStatus != "Yes")
            {
                result.Add(
                    "Consider getting a laboratory soil test for measurements such as pH and available nutrients before making major nutrient decisions.");
            }
            else
            {
                result.Add(
                    "Keep your soil-test report and use crop-specific recommendations when interpreting it.");
            }

            return string.Join(
                "\n\n",
                result);
        }


        [HttpGet]
        public IActionResult SoilResult(
            int farmId,
            int? cropId)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            FarmCrop? selectedCrop = null;

            if (cropId.HasValue)
            {
                selectedCrop =
                    _db.GetFarmCrops(
                        farm.Id,
                        userId)
                    .FirstOrDefault(
                        c => c.Id == cropId.Value);

                if (selectedCrop == null)
                {
                    return NotFound();
                }

                farm.Crop =
                    selectedCrop.CropName;
            }

            var assessment =
                _db.GetSoilAssessment(farmId);

            if (assessment == null)
            {
                return RedirectToAction(
                    nameof(SoilAssessment),
                    new
                    {
                        farmId,
                        cropId = selectedCrop?.Id
                    });
            }

            var crop =
                _cropKnowledge.GetCrop(
                    selectedCrop?.CropName
                    ?? farm.Crop);

            var model =
                new SoilAssessmentViewModel
                {
                    Farm = farm,

                    Assessment = assessment,

                    HasExistingAssessment = true,

                    CropKnowledge = crop
                };

            ViewBag.CropId =
                selectedCrop?.Id;

            ViewBag.CropName =
                selectedCrop?.CropName
                ?? farm.Crop;

            return View(model);
        }


        // =========================================================
        // ASK KRISHISAHAY AI
        // =========================================================

        // =========================================================
        // LEGACY CHECK CROP ROUTE
        // =========================================================

        [HttpGet]
        public IActionResult CheckCrop(
            int? farmId,
            int? cropId)
        {
            return RedirectToAction(
                nameof(CropDoctor),
                new { farmId, cropId });
        }
        // ============================================================
        // CROP DOCTOR
        // ============================================================
        // ============================================================
        // PUBLIC AI CROP DOCTOR
        // ============================================================

        // ============================================================
        // PUBLIC AI CROP DOCTOR
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult PublicCropDoctor()
        {
            // =========================================================
            // LOGGED-IN FARMER
            // =========================================================

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    var farms =
                        _db.GetFarmsByUserId(userId);

                    // -------------------------------------------------
                    // ONE FARM
                    // -------------------------------------------------

                    if (farms.Count == 1)
                    {
                        var farm = farms.First();

                        var crops =
                            _db.GetFarmCrops(
                                farm.Id,
                                userId);

                        // ---------------------------------------------
                        // ONE CROP
                        // Open the existing Crop Doctor directly
                        // ---------------------------------------------

                        if (crops.Count == 1)
                        {
                            return RedirectToAction(
                                nameof(CropDoctor),
                                new
                                {
                                    farmId = farm.Id,
                                    cropId = crops.First().Id
                                });
                        }
                    }

                    // -------------------------------------------------
                    // Multiple farms or crops
                    // Let farmer choose from My Farms
                    // -------------------------------------------------

                    return RedirectToAction(
                        nameof(MyFarms));
                }
            }


            // =========================================================
            // PUBLIC USER
            // =========================================================

            var model =
                new CropDoctorViewModel
                {
                    PublicMode = true
                };

            return View(
                "CropDoctor",
                model);
        }
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> PublicCropDoctor(
     CropDoctorViewModel model)
        {
            // Public Crop Doctor does not require:
            // - login
            // - farm
            // - crop selection

            model.PublicMode = true;

            // --------------------------------------------------------
            // 1. Validate image
            // --------------------------------------------------------

            if (model.CropImage == null ||
                model.CropImage.Length == 0)
            {
                model.ErrorMessage =
                    "Please upload a crop image before starting the analysis.";

                return View("CropDoctor", model);
            }

            const long maxFileSize =
                10 * 1024 * 1024;

            if (model.CropImage.Length > maxFileSize)
            {
                model.ErrorMessage =
                    "The image is too large. Please upload an image smaller than 10 MB.";

                return View("CropDoctor", model);
            }

            var allowedMimeTypes = new[]
            {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

            if (!allowedMimeTypes.Contains(
                model.CropImage.ContentType,
                StringComparer.OrdinalIgnoreCase))
            {
                model.ErrorMessage =
                    "Unsupported image format. Please upload a JPG, PNG or WEBP image.";

                return View("CropDoctor", model);
            }

            // --------------------------------------------------------
            // 2. Read uploaded image
            // --------------------------------------------------------

            byte[] imageBytes;

            using (var memoryStream = new MemoryStream())
            {
                await model.CropImage.CopyToAsync(memoryStream);

                imageBytes = memoryStream.ToArray();
            }

            // --------------------------------------------------------
            // 3. Public AI analysis
            // --------------------------------------------------------

            try
            {
                var spokenLanguageCode =
                Request.Form["SpokenLanguage"].ToString();

                var spokenLanguageInstruction =
                    string.IsNullOrWhiteSpace(spokenLanguageCode)
                        ? "Detect the language of the farmer's observation and respond completely in that same language."
                        : $"The farmer spoke in {GetAiLanguageName(spokenLanguageCode)}. Respond completely in that language, including all headings.";

                var publicContext = $"""
            PUBLIC CROP DOCTOR ANALYSIS

            No farm profile, crop selection, soil assessment,
            water assessment or location-specific information
            has been provided.

            Analyze only what can reasonably be observed from
            the uploaded crop image and the farmer's description.

            Do not invent:
            - crop identity if it cannot be reasonably determined
            - soil information
            - weather information
            - location information
            - laboratory results
            - pest or disease certainty

            Clearly distinguish visible observations from possible
            explanations.

            Provide practical next steps for the farmer.

            Do not provide pesticide, fungicide, herbicide or
            fertilizer dosage, concentration or mixing instructions.

            This is AI-assisted observation, not a definitive diagnosis.

            RESPONSE LANGUAGE
            {spokenLanguageInstruction}
            """;

                model.Analysis =
                    await _gemini.AnalyzeCropImageAsync(
                        model.Observation?.Trim() ?? "",
                        publicContext,
                        imageBytes,
                        model.CropImage.ContentType);

                model.HasAnalysis = true;
            }
            catch (Exception ex)
            {
                model.ErrorMessage =
                    "The crop analysis could not be completed right now. " +
                    "Please try again.";

                Console.WriteLine(
                    $"Public Crop Doctor error: {ex.Message}");

                model.HasAnalysis = false;
            }

            return View("CropDoctor", model);
        }

        [HttpGet]
        public IActionResult CropDoctor(
     int? farmId,
     int? cropId)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            FarmProfile? farm = null;

            if (farmId.HasValue)
            {
                farm = _db.GetFarm(
                    farmId.Value,
                    userId);
            }

            if (farm == null)
            {
                return RedirectToAction(
                    nameof(Onboarding));
            }


            // --------------------------------------------------------
            // Load the selected crop
            // --------------------------------------------------------

            FarmCrop? selectedCrop = null;

            if (cropId.HasValue)
            {
                selectedCrop =
                    _db.GetFarmCrops(
                        farm.Id,
                        userId)
                    .FirstOrDefault(
                        c => c.Id == cropId.Value);
            }


            // --------------------------------------------------------
            // Build Crop Doctor model
            // --------------------------------------------------------

            var model = new CropDoctorViewModel
            {
                Farm = farm,

                CropId = cropId,

                SelectedCrop = selectedCrop,

                PreviousObservations =
    _db.GetCropObservations(
        farm.Id,
        selectedCrop?.CropName
            ?? farm.Crop
            ?? "")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropDoctor(CropDoctorViewModel model)
        {
            // --------------------------------------------------------
            // 1. Get the farm
            // --------------------------------------------------------

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(model.Farm.Id, userId);

            if (farm == null)
            {
                return NotFound();
            }

            model.Farm = farm;
            FarmCrop? selectedCrop = null;

            if (model.CropId.HasValue)
            {
                selectedCrop = _db.GetFarmCrops(
                    farm.Id,
                    userId)
                    .FirstOrDefault(c => c.Id == model.CropId.Value);
            }

            if (selectedCrop == null)
            {
                model.ErrorMessage = "The selected crop could not be found.";
                return View(model);
            }

            model.SelectedCrop = selectedCrop;
            model.CropId = selectedCrop.Id;
            // --------------------------------------------------------
            // 2. Validate image
            // --------------------------------------------------------

            if (model.CropImage == null || model.CropImage.Length == 0)
            {
                model.ErrorMessage = "Please upload a crop image before starting the analysis.";
                return View(model);
            }

            const long maxFileSize = 10 * 1024 * 1024;

            if (model.CropImage.Length > maxFileSize)
            {
                model.ErrorMessage = "The image is too large. Please upload an image smaller than 10 MB.";
                return View(model);
            }

            var allowedMimeTypes = new[]
            {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

            if (!allowedMimeTypes.Contains(
                model.CropImage.ContentType,
                StringComparer.OrdinalIgnoreCase))
            {
                model.ErrorMessage =
                    "Unsupported image format. Please upload a JPG, PNG, or WEBP image.";

                return View(model);
            }

            // --------------------------------------------------------
            // 3. Load farm context
            // --------------------------------------------------------

            var soilAssessment = _db.GetSoilAssessment(farm.Id);
            var waterAssessment = _db.GetWaterAssessment(farm.Id);

            var crop = _cropKnowledge.GetCrop(
                selectedCrop.CropName);

            var weather =
                await _weatherService.GetWeatherAsync(farm.Location);

            // --------------------------------------------------------
            // 4. Build context for Gemini
            // --------------------------------------------------------

            var contextBuilder = new System.Text.StringBuilder();

            contextBuilder.AppendLine("FARM INFORMATION");
            contextBuilder.AppendLine($"Farm ID: {farm.Id}");
            contextBuilder.AppendLine($"Location: {farm.Location}");
            contextBuilder.AppendLine(
                $"Crop: {selectedCrop.CropName}");
            contextBuilder.AppendLine($"Farm Size: {farm.LandSize}");
            contextBuilder.AppendLine($"Irrigation: {farm.WaterSource}");
            contextBuilder.AppendLine($"Soil Type: {farm.SoilType}");
            contextBuilder.AppendLine();

            contextBuilder.AppendLine("SOIL ASSESSMENT");

            if (soilAssessment != null)
            {
                contextBuilder.AppendLine(
                    $"Soil knowledge: {soilAssessment.SoilKnowledge}");

                contextBuilder.AppendLine(
                    $"Soil texture: {soilAssessment.SoilTexture}");

                contextBuilder.AppendLine(
                    $"Soil colour: {soilAssessment.SoilColour}");

                contextBuilder.AppendLine(
                    $"Drainage: {soilAssessment.Drainage}");

                contextBuilder.AppendLine(
                    $"Previous crop: {soilAssessment.PreviousCrop}");

                contextBuilder.AppendLine(
                    $"Soil test status: {soilAssessment.SoilTestStatus}");

                contextBuilder.AppendLine(
                    $"Assessment result: {soilAssessment.Result}");
            }
            else
            {
                contextBuilder.AppendLine("No soil assessment is available.");
            }

            contextBuilder.AppendLine();

            contextBuilder.AppendLine("WATER ASSESSMENT");

            if (waterAssessment != null)
            {
                contextBuilder.AppendLine(
                    $"Water source: {waterAssessment.WaterSource}");

                contextBuilder.AppendLine(
                    $"Water availability: {waterAssessment.WaterAvailability}");

                contextBuilder.AppendLine(
                    $"Irrigation method: {waterAssessment.IrrigationMethod}");

                contextBuilder.AppendLine(
                    $"Water quality knowledge: {waterAssessment.WaterQualityKnowledge}");

                contextBuilder.AppendLine(
                    $"Water assessment result: {waterAssessment.Result}");
            }
            else
            {
                contextBuilder.AppendLine("No water assessment is available.");
            }

            contextBuilder.AppendLine();

            // --------------------------------------------------------
            // 5. Add live weather
            // --------------------------------------------------------

            contextBuilder.AppendLine("LIVE WEATHER");

            if (weather != null)
            {
                contextBuilder.AppendLine(
                    $"Location: {weather.LocationName}");

                contextBuilder.AppendLine(
                    $"Temperature: {weather.TemperatureC:F1} °C");

                contextBuilder.AppendLine(
                    $"Humidity: {weather.HumidityPercent:F0}%");

                contextBuilder.AppendLine(
                    $"Wind speed: {weather.WindSpeedKmh:F1} km/h");

                contextBuilder.AppendLine(
                    $"Current precipitation: {weather.PrecipitationMm:F1} mm");

                contextBuilder.AppendLine(
                    $"Current conditions: {weather.WeatherDescription}");

                contextBuilder.AppendLine();

                contextBuilder.AppendLine("7-DAY FORECAST");

                foreach (var day in weather.Forecast)
                {
                    contextBuilder.AppendLine(
                        $"{day.Date:yyyy-MM-dd}: " +
                        $"{day.WeatherDescription}, " +
                        $"High {day.MaxTemperatureC:F1} °C, " +
                        $"Low {day.MinTemperatureC:F1} °C, " +
                        $"Rain probability {day.PrecipitationProbabilityPercent:F0}%, " +
                        $"Rainfall {day.PrecipitationMm:F1} mm");
                }
            }
            else
            {
                contextBuilder.AppendLine(
                    "Live weather information is currently unavailable.");
            }

            contextBuilder.AppendLine();

            // --------------------------------------------------------
            // 6. Add crop knowledge
            // --------------------------------------------------------

            contextBuilder.AppendLine("CROP KNOWLEDGE");

            if (crop != null)
            {
                contextBuilder.AppendLine($"Crop: {crop.CropName}");
                contextBuilder.AppendLine($"Overview: {crop.Overview}");
                contextBuilder.AppendLine(
                    $"Soil requirements: {crop.SoilRequirements}");
                contextBuilder.AppendLine(
                    $"Water requirements: {crop.WaterRequirements}");
                contextBuilder.AppendLine(
                    $"Climate requirements: {crop.ClimateRequirements}");
                contextBuilder.AppendLine(
                    $"Planting guidance: {crop.PlantingGuidance}");
                contextBuilder.AppendLine(
                    $"Growth guidance: {crop.GrowthGuidance}");
                contextBuilder.AppendLine(
                    $"Harvest guidance: {crop.HarvestGuidance}");
                contextBuilder.AppendLine(
                    $"Important note: {crop.ImportantNote}");
            }
            else
            {
                contextBuilder.AppendLine(
                    "No crop-specific knowledge is available.");
            }

            var farmContext = contextBuilder.ToString();

            // --------------------------------------------------------
            // 7. Read uploaded image
            // --------------------------------------------------------

            byte[] imageBytes;

            using (var memoryStream = new MemoryStream())
            {
                await model.CropImage.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            // --------------------------------------------------------
            // 8. Send image + context to Gemini
            // --------------------------------------------------------

            try
            {
                var spokenLanguageCode =
                    Request.Form["SpokenLanguage"].ToString();

                var spokenLanguageInstruction =
                    string.IsNullOrWhiteSpace(spokenLanguageCode)
                        ? "Detect the language of the farmer's observation and respond completely in that same language."
                        : $"The farmer spoke in {GetAiLanguageName(spokenLanguageCode)}. Respond completely in that language, including all headings.";

                farmContext += $"\n\nRESPONSE LANGUAGE\n{spokenLanguageInstruction}";

                model.Analysis =
                    await _gemini.AnalyzeCropImageAsync(
                        model.Observation?.Trim() ?? "",
                        farmContext,
                        imageBytes,
                        model.CropImage.ContentType);
                // --------------------------------------------------------
                // 9. Save the uploaded image for crop history
                // --------------------------------------------------------

                var extension = model.CropImage.ContentType
                    .ToLowerInvariant() switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    _ => ".jpg"
                };

                var relativeFolder =
                    Path.Combine(
                        "uploads",
                        "crop-doctor",
                        $"farm-{farm.Id}",
                        $"crop-{selectedCrop.Id}");

                var physicalFolder =
                    Path.Combine(
                        _environment.WebRootPath,
                        relativeFolder);

                Directory.CreateDirectory(physicalFolder);

                var fileName =
                    $"{Guid.NewGuid():N}{extension}";

                var physicalFilePath =
                    Path.Combine(
                        physicalFolder,
                        fileName);

                using (var fileStream =
                       new FileStream(
                           physicalFilePath,
                           FileMode.Create))
                {
                    await model.CropImage.CopyToAsync(fileStream);
                }

                var imagePath =
                    "/" +
                    relativeFolder
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(Path.AltDirectorySeparatorChar, '/') +
                    "/" +
                    fileName;
                _db.SaveCropObservation(
                    farm.Id,
                    selectedCrop.CropName,
                    imagePath,
                    model.Observation?.Trim() ?? "",
                    model.Analysis);

                // Reload Crop Doctor history so the newly saved
                // and previous analyses appear immediately.
                model.PreviousObservations =
                    _db.GetCropObservations(
                        farm.Id,
                        selectedCrop.CropName);


                // =====================================================
                // COMPLETE CROP MONITORING MILESTONE
                // A saved crop observation means the farmer has started
                // monitoring crop growth.
                // =====================================================

                var monitoringActivity =
                    _db.GetActivities(farm.Id)
                        .FirstOrDefault(
                            a =>
                                a.Title ==
                                "Monitor crop growth");

                if (monitoringActivity == null)
                {
                    // Safety for older farms where the milestone
                    // may not exist yet.
                    _db.AddActivity(
                        new FarmActivity
                        {
                            FarmProfileId = farm.Id,

                            Title =
                                "Monitor crop growth",

                            Description =
                                "Review crop growth and look for changes.",

                            ActivityDate =
                                DateTime.Today.AddDays(14),

                            Category =
                                "Monitoring"
                        });

                    monitoringActivity =
                        _db.GetActivities(farm.Id)
                            .FirstOrDefault(
                                a =>
                                    a.Title ==
                                    "Monitor crop growth");
                }

                if (monitoringActivity != null)
                {
                    _db.UpdateActivityCompletion(
                        monitoringActivity.Id,
                        farm.Id,
                        true);
                }


                model.HasAnalysis = true;
            }
            catch (Exception ex)
            {
                model.ErrorMessage =
                    "The crop analysis could not be completed right now. " +
                    "Please try again.";

                Console.WriteLine(
                    $"Crop Doctor error: {ex.Message}");

                model.HasAnalysis = false;
            }

            return View(model);
        }
        // ============================================================
        // CROP DASHBOARD
        // ============================================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CropDashboard(
            int farmId,
            int cropId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // =========================================================
            // LOAD FARM
            // =========================================================

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }


            // =========================================================
            // LOAD ALL CROPS
            // =========================================================

            var crops =
                _db.GetFarmCrops(
                    farmId,
                    userId);


            // =========================================================
            // LOAD SELECTED CROP
            // =========================================================

            var crop =
                crops.FirstOrDefault(
                    c => c.Id == cropId);

            if (crop == null)
            {
                return NotFound();
            }


            ViewBag.Farm = farm;


            // =========================================================
            // GEMINI FARM BUDGET ALLOCATION
            // =========================================================

            ViewBag.GeminiBudgetAllocation = null;

            ViewBag.GeminiBudgetError = null;


            try
            {
                // -----------------------------------------------------
                // BUILD COMPLETE FARM + ALL CROPS CONTEXT
                // -----------------------------------------------------

                var cropInformation =
                    string.Join(
                        "\n\n",
                        crops.Select(
                            c =>
                                $"""
                        Crop Name: {c.CropName}
                        Crop Area: {c.Area:0.##} {c.AreaUnit}
                        Variety: {c.Variety ?? "Not specified"}
                        Season: {c.Season ?? "Not specified"}
                        Sowing Date: {(c.SowingDate.HasValue
                                    ? c.SowingDate.Value.ToString("dd MMM yyyy")
                                    : "Not specified")}
                        Notes: {c.Notes ?? "None"}
                        """));


                var farmContext =
                    $"""
            FARM INFORMATION
            ========================================

            Farmer Name:
            {farm.FarmerName}

            Location:
            {farm.Location}

            Total Farm Area:
            {farm.LandSize:0.##} {farm.LandUnit}

            Total Farm Budget:
            ₹{farm.Budget:N0}

            Soil Type:
            {farm.SoilType ?? "Not specified"}

            Water Source:
            {farm.WaterSource ?? "Not specified"}

            Farming Method:
            {farm.FarmingMethod ?? "Not specified"}

            Experience Level:
            {farm.ExperienceLevel ?? "Not specified"}


            ========================================
            ALL CROPS ON THIS FARM
            ========================================

            {cropInformation}


            ========================================
            SELECTED CROP
            ========================================

            {crop.CropName}
            {crop.Area:0.##} {crop.AreaUnit}


            ========================================
            INSTRUCTION
            ========================================

            Allocate the TOTAL farm budget across ALL
            crops.

            The total allocation across all crops MUST
            equal exactly ₹{farm.Budget:N0}.

            Consider crop-specific requirements,
            crop area and farm context.

            Do not allocate the complete farm budget
            independently to every crop.

            Return the allocation for every crop.
            """;


                // -----------------------------------------------------
                // CALL GEMINI
                // -----------------------------------------------------

                var geminiResponse =
                    await _gemini.AllocateFarmBudgetAsync(
                        farmContext);


                // -----------------------------------------------------
                // STORE GEMINI RESPONSE
                // -----------------------------------------------------

                ViewBag.GeminiBudgetAllocation =
                    ParseGeminiBudgetAllocation(
                        geminiResponse,
                        crop,
                        farm.Budget);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Gemini Budget Allocation Error: {ex.Message}");

                ViewBag.GeminiBudgetError =
                    "Gemini budget allocation is currently unavailable.";
            }


            return View(crop);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AskAI(
      int? farmId,
      int? cropId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            // =========================================================
            // GENERAL AI MODE
            // =========================================================
            // General questions are available to everyone.
            // =========================================================

            if (!farmId.HasValue)
            {
                var generalModel =
                    new AiChatViewModel
                    {
                        AvailableFarms =
                            string.IsNullOrEmpty(userId)
                                ? new List<FarmProfile>()
                                : _db.GetFarmsByUserId(userId),

                        IsGeneralQuestion = true
                    };

                return View(generalModel);
            }

            // =========================================================
            // FARM-SPECIFIC AI REQUIRES LOGIN
            // =========================================================

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // =========================================================
            // LOAD ALL FARMS BELONGING TO THE LOGGED-IN USER
            // =========================================================

            var farms =
                _db.GetFarmsByUserId(userId);

            var model =
                new AiChatViewModel
                {
                    AvailableFarms = farms,
                    IsGeneralQuestion = !farmId.HasValue
                };



            // =========================================================
            // FARM-SPECIFIC MODE
            // =========================================================

            var farm =
                _db.GetFarm(
                    farmId.Value,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            model.Farm = farm;
            model.SelectedFarmId = farm.Id;

            model.AvailableCrops =
                _db.GetFarmCrops(
                    farm.Id,
                    userId);


            // =========================================================
            // CROP-SPECIFIC MODE
            // =========================================================

            if (cropId.HasValue)
            {
                var selectedCrop =
                    model.AvailableCrops
                        .FirstOrDefault(
                            c => c.Id == cropId.Value);

                if (selectedCrop == null)
                {
                    return NotFound();
                }

                model.SelectedCrop =
                    selectedCrop;

                model.SelectedCropId =
                    selectedCrop.Id;

                // Keep the selected crop in the existing Farm object
                // for compatibility with the current AI context.
                model.Farm.Crop =
                    selectedCrop.CropName;

                TempData["AskAICropId"] =
                    selectedCrop.Id;
            }
            else
            {
                TempData.Remove(
                    "AskAICropId");
            }


            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskAI(
     AiChatViewModel model)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // =========================================================
            // RESPONSE LANGUAGE
            // =========================================================

            var selectedLanguage =
                GetAiLanguageName(model.SelectedLanguage);

            // =========================================================
            // LOAD FARMS FOR LOGGED-IN USERS
            // =========================================================

            if (!string.IsNullOrEmpty(userId))
            {
                model.AvailableFarms =
                    _db.GetFarmsByUserId(userId);
            }
            else
            {
                model.AvailableFarms =
                    new List<FarmProfile>();
            }


            // =========================================================
            // GENERAL FARMING QUESTION
            // =========================================================

            if (!model.SelectedFarmId.HasValue)
            {
                model.IsGeneralQuestion = true;
                model.Farm = new FarmProfile();
                model.AvailableCrops = new List<FarmCrop>();

                if (string.IsNullOrWhiteSpace(model.Question))
                {
                    model.Answer =
                        "Please enter a farming question before asking KrishiSahay AI.";

                    model.HasAnswer = true;

                    return View(model);
                }

                var generalContext = $"""
                    You are KrishiSahay AI, an agricultural decision-support assistant.

                    The farmer is asking a GENERAL farming question and has not selected
                    a specific farm or crop.

                    Give practical, understandable and farmer-friendly guidance.

                    When relevant, consider: soil management, water management, crop
                    selection, planting, crop growth, weather, pest and disease observation,
                    sustainable farming and regenerative agricultural practices.

                    Do not invent farm conditions, measurements, weather, soil results or
                    crop information that the farmer did not provide.

                    For serious crop disease, pest identification, fertilizer dosage,
                    pesticide or chemical-use decisions, recommend verification with an
                    appropriate agricultural expert and locally applicable agricultural
                    recommendations.

                    If the question depends on a specific crop, location or farm condition,
                    explain what additional information would be useful.

                    Answer in simple farmer-friendly language.

                    RESPONSE LANGUAGE
                    ----------------
                    Respond in {selectedLanguage}.
                    Use that language for the complete answer unless the farmer's
                    question clearly requires a technical term that is better kept
                    in its commonly used original form.
                    """;

                try
                {
                    model.Answer =
                        await _gemini.AskAsync(
                            model.Question.Trim(),
                            generalContext);

                    model.HasAnswer = true;
                }
                catch (Exception ex)
                {
                    model.Answer =
                        "KrishiSahay AI could not process your question right now. Please try again later.";

                    model.HasAnswer = true;

                    Console.WriteLine(
                        $"AskAI General Error: {ex.Message}");
                }

                return View(model);

            }

            // =========================================================
            // FARM-SPECIFIC AI REQUIRES LOGIN
            // =========
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // =========================================================
            // FARM-SPECIFIC QUESTION
            // =========================================================

            var farm =
                _db.GetFarm(
                    model.SelectedFarmId.Value,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            model.Farm = farm;
            model.SelectedFarmId = farm.Id;
            model.IsGeneralQuestion = false;


            // =========================================================
            // LOAD CROPS FOR THE SELECTED FARM
            // =========================================================

            model.AvailableCrops =
                _db.GetFarmCrops(
                    farm.Id,
                    userId);


            // =========================================================
            // CROP IS REQUIRED FOR FARM-SPECIFIC AI
            // =========================================================

            if (!model.SelectedCropId.HasValue)
            {
                model.SelectedCrop = null;
                model.Answer =
                    "Please select a crop before asking a farm-specific question.";

                model.HasAnswer = true;

                return View(model);
            }


            // =========================================================
            // LOAD SELECTED CROP
            // =========================================================

            var selectedCrop =
                model.AvailableCrops
                    .FirstOrDefault(
                        c => c.Id == model.SelectedCropId.Value);

            if (selectedCrop == null)
            {
                return NotFound();
            }

            model.SelectedCrop = selectedCrop;
            model.SelectedCropId = selectedCrop.Id;

            // Keep compatibility with existing farm-level AI context.
            model.Farm.Crop = selectedCrop.CropName;


            // =========================================================
            // QUESTION VALIDATION
            // =========================================================

            if (string.IsNullOrWhiteSpace(model.Question))
            {
                model.Answer =
                    "Please enter a question about your farm or selected crop.";

                model.HasAnswer = true;

                return View(model);
            }


            // =====================================================
            // SOIL INFORMATION
            // =====================================================

            var soilAssessment =
                _db.GetSoilAssessment(farm.Id);


            // =====================================================
            // WATER INFORMATION
            // =====================================================

            var waterAssessment =
                _db.GetWaterAssessment(farm.Id);


            // =====================================================
            // CROP KNOWLEDGE
            // =====================================================

            var cropName =
                selectedCrop.CropName;

            var crop =
                _cropKnowledge.GetCrop(
                    cropName);


            // =====================================================
            // LIVE WEATHER
            // =====================================================

            var weather =
                await _weatherService.GetWeatherAsync(
                    farm.Location);


            // =====================================================
            // COMPLETE FARM CONTEXT FOR GEMINI
            // =====================================================

            var farmContext = $"""
                You are KrishiSahay AI, an agricultural decision-support assistant helping a farmer.

                Use the following information as context for the farmer's question.

                FARM INFORMATION
                ----------------
                Farm ID: {farm.Id}
                Farmer: {farm.FarmerName}
                Location: {farm.Location}
                Land Area: {farm.LandSize} {farm.LandUnit}
                Water Source from Farm Profile: {farm.WaterSource}
                Farming Method: {farm.FarmingMethod}
                Experience Level: {farm.ExperienceLevel}
                Budget: {farm.Budget}

                SELECTED CROP
                ----------------
                Crop: {selectedCrop.CropName}
                Variety: {selectedCrop.Variety}
                Season: {selectedCrop.Season}
                Area: {selectedCrop.Area} {selectedCrop.AreaUnit}
                Sowing Date: {selectedCrop.SowingDate:dd MMM yyyy}
                Notes: {selectedCrop.Notes}

                SOIL ASSESSMENT
                ----------------
                Soil Knowledge:
                {soilAssessment?.SoilKnowledge ?? "Not assessed"}

                Soil Texture:
                {soilAssessment?.SoilTexture ?? "Not assessed"}

                Soil Colour:
                {soilAssessment?.SoilColour ?? "Not assessed"}

                Soil Drainage:
                {soilAssessment?.Drainage ?? "Not assessed"}

                Previous Crop:
                {soilAssessment?.PreviousCrop ?? "Not recorded"}

                Soil Test Status:
                {soilAssessment?.SoilTestStatus ?? "Not recorded"}


                WATER ASSESSMENT
                ----------------
                Water Source:
                {waterAssessment?.WaterSource ?? "Not assessed"}

                Water Availability:
                {waterAssessment?.WaterAvailability ?? "Not assessed"}

                Irrigation Method:
                {waterAssessment?.IrrigationMethod ?? "Not assessed"}

                Irrigation Frequency:
                {waterAssessment?.IrrigationFrequency ?? "Not assessed"}

                Water Quality Knowledge:
                {waterAssessment?.WaterQualityKnowledge ?? "Not assessed"}

                Drainage Condition:
                {waterAssessment?.DrainageCondition ?? "Not assessed"}

                Recent Water Problem:
                {waterAssessment?.RecentWaterProblem ?? "Not assessed"}


                LIVE WEATHER
                ----------------
                Weather Location:
                {weather?.LocationName ?? "Not available"}

                Current Temperature:
                {(weather != null ? weather.TemperatureC.ToString("0.#") + " °C" : "Not available")}

                Current Humidity:
                {(weather != null ? weather.HumidityPercent.ToString("0") + "%" : "Not available")}

                Current Wind Speed:
                {(weather != null ? weather.WindSpeedKmh.ToString("0.#") + " km/h" : "Not available")}

                Current Precipitation:
                {(weather != null ? weather.PrecipitationMm.ToString("0.#") + " mm" : "Not available")}

                Current Weather:
                {weather?.WeatherDescription ?? "Not available"}

                Weather Code:
                {weather?.WeatherCode.ToString() ?? "Not available"}

                Day / Night:
                {(weather != null ? (weather.IsDay ? "Day" : "Night") : "Not available")}

                7-DAY FORECAST
                ----------------
                {(weather != null
                    ? string.Join(
                        "\n",
                        weather.Forecast.Select(day =>
                            $"{day.Date:dd MMM}: {day.WeatherDescription}, " +
                            $"Min {day.MinTemperatureC:0.#} °C, " +
                            $"Max {day.MaxTemperatureC:0.#} °C, " +
                            $"Rain Probability {day.PrecipitationProbabilityPercent:0}%, " +
                            $"Rainfall {day.PrecipitationMm:0.#} mm"))
                    : "Weather forecast not available")}

                CROP KNOWLEDGE
                ----------------
                Crop:
                {crop?.CropName ?? cropName}

                Overview:
                {crop?.Overview ?? "Not available"}

                Soil Requirements:
                {crop?.SoilRequirements ?? "Not available"}

                Water Requirements:
                {crop?.WaterRequirements ?? "Not available"}

                Climate Requirements:
                {crop?.ClimateRequirements ?? "Not available"}

                Planting Guidance:
                {crop?.PlantingGuidance ?? "Not available"}

                Growth Guidance:
                {crop?.GrowthGuidance ?? "Not available"}

                Harvest Guidance:
                {crop?.HarvestGuidance ?? "Not available"}

                Important Note:
                {crop?.ImportantNote ?? "Not available"}

                INSTRUCTIONS
                ----------------
                Answer specifically for the selected crop and farm.

                Use the available farm, crop, soil, water and weather information when relevant.

                Keep the answer practical and understandable for a farmer.

                Do not invent measurements, symptoms, weather conditions or test results that are not present in the provided context.

                If information is insufficient, clearly say what additional information would be useful.

                For serious crop disease, pest identification, fertilizer dosage, pesticide or chemical-use decisions, recommend verification with an appropriate agricultural expert and locally applicable agricultural recommendations.

                RESPONSE LANGUAGE
                ----------------
                Respond in {selectedLanguage}.
                Use that language for the complete answer unless the farmer's
                question clearly requires a technical term that is better kept
                in its commonly used original form.
                """;


            // =====================================================
            // ASK GEMINI
            // =====================================================

            try
            {
                model.Answer =
                    await _gemini.AskAsync(
                        model.Question.Trim(),
                        farmContext);

                model.HasAnswer = true;
            }
            catch (Exception ex)
            {
                model.Answer =
                    "KrishiSahay AI could not process your question right now. Please try again later.";

                model.HasAnswer = true;

                Console.WriteLine(
                    $"AskAI Farm Error: {ex.Message}");
            }

            return View(model);
        }

        // =========================================================
        // VOICE TRANSCRIPTION
        // =========================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> TranscribeSpeech(IFormFile? audio)
        {
            if (audio == null || audio.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No audio recording was received."
                });
            }

            if (audio.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The audio recording is too large."
                });
            }

            var allowedMimeTypes = new[]
            {
                "audio/webm",
                "audio/opus",
                "audio/ogg",
                "audio/wav",
                "audio/mp3",
                "audio/mpeg",
                "audio/mp4",
                "audio/m4a",
                "audio/aac",
                "audio/flac"
            };

            var mimeType =
                string.IsNullOrWhiteSpace(audio.ContentType)
                    ? "audio/webm"
                    : audio.ContentType.Split(';')[0].Trim();

            if (!allowedMimeTypes.Contains(
                    mimeType,
                    StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unsupported audio format."
                });
            }

            try
            {
                byte[] audioBytes;

                using (var memoryStream = new MemoryStream())
                {
                    await audio.CopyToAsync(memoryStream);
                    audioBytes = memoryStream.ToArray();
                }

                var result =
                    await _gemini.TranscribeAudioAsync(
                        audioBytes,
                        mimeType);

                return Json(new
                {
                    success = true,
                    transcript = result.Transcript,
                    languageCode = result.LanguageCode,
                    languageName = GetAiLanguageName(result.LanguageCode)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Voice transcription error: {ex.Message}");

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = "Voice transcription could not be completed right now. Please try again."
                    });
            }
        }

        private static string GetAiLanguageName(string? languageCode)
        {
            return languageCode switch
            {
                "gu" => "Gujarati",
                "hi" => "Hindi",
                "mr" => "Marathi",
                "bn" => "Bengali",
                "kn" => "Kannada",
                "ml" => "Malayalam",
                "pa" => "Punjabi",
                "ta" => "Tamil",
                "te" => "Telugu",
                "auto" => "the same language used by the farmer. Automatically detect the language of the farmer's question and respond completely in that language",
                _ => "English"
            };
        }


        private static (
    double Allocated,
    double Remaining,
    bool CanConvert
)
CalculateFarmArea(
    FarmProfile farm,
    List<FarmCrop> crops)
        {
            if (farm.LandSize <= 0)
            {
                return (
                    0,
                    0,
                    false);
            }


            // -----------------------------
            // STANDARD UNITS
            // -----------------------------

            if (AreaUnitConverter.TryToAcres(
                farm.LandSize,
                farm.LandUnit,
                out var farmAcres))
            {
                var allocatedAcres = 0d;


                foreach (var crop in crops)
                {
                    if (!AreaUnitConverter.TryToAcres(
                        crop.Area,
                        crop.AreaUnit,
                        out var cropAcres))
                    {
                        return (
                            0,
                            farm.LandSize,
                            false);
                    }

                    allocatedAcres +=
                        cropAcres;
                }


                var farmUnitFactor =
                    farmAcres /
                    farm.LandSize;


                return (
                    allocatedAcres /
                        farmUnitFactor,

                    Math.Max(
                        0,
                        (farmAcres -
                         allocatedAcres) /
                        farmUnitFactor),

                    true);
            }


            // -----------------------------
            // BIGHA
            // -----------------------------

            if (
                AreaUnitConverter.IsSameUnit(
                    farm.LandUnit,
                    "Bigha")

                &&

                crops.All(
                    c =>
                        AreaUnitConverter.IsSameUnit(
                            c.AreaUnit,
                            farm.LandUnit))
            )
            {
                var allocated =
                    crops.Sum(
                        c => c.Area);


                return (
                    allocated,

                    Math.Max(
                        0,
                        farm.LandSize -
                        allocated),

                    true);
            }


            return (
                0,
                farm.LandSize,
                false);
        }
        private static object? ParseGeminiBudgetAllocation(
    string response,
    FarmCrop selectedCrop,
    decimal totalFarmBudget)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return null;
            }

            try
            {
                var json =
                    response.Trim();


                // -----------------------------------------------------
                // REMOVE MARKDOWN CODE FENCES IF GEMINI RETURNS THEM
                // -----------------------------------------------------

                if (json.StartsWith("```"))
                {
                    var firstNewLine =
                        json.IndexOf('\n');

                    var lastFence =
                        json.LastIndexOf("```");

                    if (firstNewLine >= 0 &&
                        lastFence > firstNewLine)
                    {
                        json =
                            json.Substring(
                                firstNewLine + 1,
                                lastFence -
                                firstNewLine -
                                1)
                            .Trim();
                    }
                }


                using var document =
                    JsonDocument.Parse(json);

                var root =
                    document.RootElement;


                // -----------------------------------------------------
                // GET TOTAL FARM BUDGET FROM GEMINI
                // -----------------------------------------------------

                if (!root.TryGetProperty(
                        "totalFarmBudget",
                        out var totalBudgetElement))
                {
                    return null;
                }


                var returnedTotal =
                    totalBudgetElement.GetDecimal();


                if (returnedTotal <= 0)
                {
                    return null;
                }


                // -----------------------------------------------------
                // GET ALLOCATIONS
                // -----------------------------------------------------

                if (!root.TryGetProperty(
                        "allocations",
                        out var allocationsElement)
                    ||
                    allocationsElement.ValueKind !=
                    JsonValueKind.Array)
                {
                    return null;
                }


                // -----------------------------------------------------
                // VERIFY ALL GEMINI ALLOCATIONS
                // EQUAL THE COMPLETE FARM BUDGET
                // -----------------------------------------------------

                decimal allocationSum = 0;


                foreach (
                    var allocation
                    in allocationsElement.EnumerateArray())
                {
                    if (allocation.TryGetProperty(
                            "allocatedAmount",
                            out var amountElement))
                    {
                        allocationSum +=
                            amountElement.GetDecimal();
                    }
                }


                if (Math.Abs(
                        allocationSum -
                        totalFarmBudget) > 1m)
                {
                    Console.WriteLine(
                        "Gemini budget allocation rejected because " +
                        "the crop allocations do not equal the " +
                        "complete farm budget.");

                    return null;
                }


                // -----------------------------------------------------
                // FIND CURRENT CROP
                // -----------------------------------------------------

                foreach (
                    var allocation
                    in allocationsElement.EnumerateArray())
                {
                    if (!allocation.TryGetProperty(
                            "cropName",
                            out var cropNameElement))
                    {
                        continue;
                    }


                    var cropName =
                        cropNameElement.GetString();


                    if (string.IsNullOrWhiteSpace(
                            cropName))
                    {
                        continue;
                    }


                    if (!string.Equals(
                            cropName.Trim(),
                            selectedCrop.CropName?.Trim(),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }


                    decimal allocatedAmount = 0;


                    if (allocation.TryGetProperty(
                            "allocatedAmount",
                            out var amountElement))
                    {
                        allocatedAmount =
                            amountElement.GetDecimal();
                    }


                    decimal percentage =
                        totalFarmBudget > 0
                            ? (allocatedAmount /
                               totalFarmBudget) *
                               100m
                            : 0;


                    return new
                    {
                        CropName =
                            selectedCrop.CropName,

                        AllocatedAmount =
                            allocatedAmount,

                        Percentage =
                            percentage,

                        TotalFarmBudget =
                            totalFarmBudget
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Gemini budget JSON parse error: {ex.Message}");
            }


            return null;
        }

        private static bool
TryCalculateCropAreaAgainstFarm(
    FarmProfile farm,
    List<FarmCrop> existingCrops,
    double newCropArea,
    string newCropUnit,
    out double allocatedAfterAdd,
    out double remainingAfterAdd,
    out string error)
        {
            allocatedAfterAdd = 0;

            remainingAfterAdd = 0;

            error = "";


            if (newCropArea <= 0)
            {
                error =
                    "Crop area must be greater than 0.";

                return false;
            }


            // -----------------------------
            // STANDARD UNIT CALCULATION
            // -----------------------------

            if (
                AreaUnitConverter.TryToAcres(
                    farm.LandSize,
                    farm.LandUnit,
                    out var farmAcres)

                &&

                AreaUnitConverter.TryToAcres(
                    newCropArea,
                    newCropUnit,
                    out var newCropAcres))
            {
                var allocatedAcres = 0d;


                foreach (var crop in existingCrops)
                {
                    if (!AreaUnitConverter.TryToAcres(
                        crop.Area,
                        crop.AreaUnit,
                        out var cropAcres))
                    {
                        error =
                            $"The existing crop " +
                            $"'{crop.CropName}' uses an " +
                            $"area unit that cannot be " +
                            $"converted safely. " +
                            $"Please use Acre, Hectare, " +
                            $"Guntha, Square Meter or " +
                            $"Square Feet.";

                        return false;
                    }


                    allocatedAcres +=
                        cropAcres;
                }


                var totalAfterAddAcres =
                    allocatedAcres +
                    newCropAcres;


                var farmUnitFactor =
                    farmAcres /
                    farm.LandSize;


                allocatedAfterAdd =
                    totalAfterAddAcres /
                    farmUnitFactor;


                remainingAfterAdd =
                    (farmAcres -
                     totalAfterAddAcres) /
                    farmUnitFactor;


                return true;
            }


            // -----------------------------
            // BIGHA CALCULATION
            // -----------------------------

            if (
                AreaUnitConverter.IsSameUnit(
                    farm.LandUnit,
                    "Bigha")

                &&

                AreaUnitConverter.IsSameUnit(
                    newCropUnit,
                    "Bigha")

                &&

                existingCrops.All(
                    c =>
                        AreaUnitConverter.IsSameUnit(
                            c.AreaUnit,
                            "Bigha"))
            )
            {
                allocatedAfterAdd =
                    existingCrops.Sum(
                        c => c.Area)
                    +
                    newCropArea;


                remainingAfterAdd =
                    farm.LandSize -
                    allocatedAfterAdd;


                return true;
            }


            error =
                "Farm and crop area units cannot be " +
                "compared safely. Please use the same " +
                "unit for Bigha, or use Acre, Hectare, " +
                "Guntha, Square Meter or Square Feet.";

            return false;
        }

        // =========================================================
        // EDIT CROP - GET
        // =========================================================

        [Authorize]
        [HttpGet]
        public IActionResult EditCrop(
            int farmId,
            int cropId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            var crop =
                _db.GetFarmCrop(
                    cropId,
                    farmId,
                    userId);

            if (crop == null)
            {
                return NotFound();
            }

            var crops =
                _db.GetFarmCrops(
                    farmId,
                    userId);

            var otherCrops =
                crops
                    .Where(c => c.Id != cropId)
                    .ToList();

            var areaInfo =
                CalculateFarmArea(
                    farm,
                    otherCrops);

            ViewBag.Farm =
                farm;

            ViewBag.AllocatedArea =
                areaInfo.Allocated;

            ViewBag.RemainingArea =
                areaInfo.Remaining;

            ViewBag.AreaUnit =
                farm.LandUnit;

            ViewBag.AreaCanBeConverted =
                areaInfo.CanConvert;

            return View(crop);
        }

        // =========================================================
        // EDIT CROP - POST
        // =========================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCrop(
            FarmCrop model)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    model.FarmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            var existingCrop =
                _db.GetFarmCrop(
                    model.Id,
                    model.FarmId,
                    userId);

            if (existingCrop == null)
            {
                return NotFound();
            }


            // -----------------------------
            // BASIC VALIDATION
            // -----------------------------

            if (string.IsNullOrWhiteSpace(
                model.CropName))
            {
                ModelState.AddModelError(
                    nameof(model.CropName),
                    "Please enter a crop name.");
            }

            if (model.Area <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.Area),
                    "Please enter a crop area greater than 0.");
            }


            // -----------------------------
            // AREA VALIDATION
            // IMPORTANT:
            // Exclude the crop currently
            // being edited.
            // -----------------------------

            var allCrops =
                _db.GetFarmCrops(
                    model.FarmId,
                    userId);

            var otherCrops =
                allCrops
                    .Where(c => c.Id != model.Id)
                    .ToList();

            var areaInfo =
                CalculateFarmArea(
                    farm,
                    otherCrops);


            if (!TryCalculateCropAreaAgainstFarm(
                farm,
                otherCrops,
                model.Area,
                model.AreaUnit,
                out var allocatedAfterAdd,
                out var remainingAfterAdd,
                out var areaError))
            {
                ModelState.AddModelError(
                    nameof(model.Area),
                    areaError);
            }
            else if (remainingAfterAdd < -0.000001)
            {
                ModelState.AddModelError(
                    nameof(model.Area),
                    $"Farm area exceeded. " +
                    $"You only have " +
                    $"{Math.Max(0, areaInfo.Remaining):0.##} " +
                    $"{farm.LandUnit} available.");
            }


            // -----------------------------
            // RETURN FORM IF INVALID
            // -----------------------------

            if (!ModelState.IsValid)
            {
                ViewBag.Farm =
                    farm;

                ViewBag.AllocatedArea =
                    areaInfo.Allocated;

                ViewBag.RemainingArea =
                    areaInfo.Remaining;

                ViewBag.AreaUnit =
                    farm.LandUnit;

                ViewBag.AreaCanBeConverted =
                    areaInfo.CanConvert;

                return View(model);
            }


            // -----------------------------
            // PRESERVE OWNERSHIP
            // -----------------------------

            model.UserId =
                userId;

            model.CreatedAt =
                existingCrop.CreatedAt;


            _db.UpdateFarmCrop(
                model);


            return RedirectToAction(
                nameof(MyCrops),
                new
                {
                    farmId = model.FarmId
                });
        }

        // =========================================================
        // DELETE CROP
        // =========================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCrop(
            int farmId,
            int cropId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var farm =
                _db.GetFarm(
                    farmId,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }

            var crop =
                _db.GetFarmCrop(
                    cropId,
                    farmId,
                    userId);

            if (crop == null)
            {
                return NotFound();
            }


            _db.DeleteFarmCrop(
                cropId,
                farmId,
                userId);


            return RedirectToAction(
                nameof(MyCrops),
                new
                {
                    farmId = farmId
                });
        }


    }
}
