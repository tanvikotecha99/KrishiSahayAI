using KrishiSahayAI.Data;
using KrishiSahayAI.Models;
using KrishiSahayAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KrishiSahayAI.Controllers
{
    public class WeatherController : Controller
    {
        private readonly AppDb _db;
        private readonly IWeatherService _weatherService;
        private readonly ICropKnowledgeService _cropKnowledge;

        public WeatherController(
            AppDb db,
            IWeatherService weatherService,
            ICropKnowledgeService cropKnowledge)
        {
            _db = db;
            _weatherService = weatherService;
            _cropKnowledge = cropKnowledge;
        }


        // =========================================================
        // WEATHER ENTRY
        // GET: /Weather
        //
        // Logged out:
        //     → Local Weather page
        //
        // Logged in + no farms:
        //     → Local Weather page
        //
        // Logged in + one farm:
        //     → Directly opens that farm's weather
        //
        // Logged in + multiple farms:
        //     → Farm selection page
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Select()
        {
            // -----------------------------------------------------
            // USER IS NOT LOGGED IN
            // -----------------------------------------------------

            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return View("LocalWeather");
            }


            // -----------------------------------------------------
            // GET CURRENT USER ID
            // -----------------------------------------------------

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return View("LocalWeather");
            }


            // -----------------------------------------------------
            // GET USER FARMS
            // -----------------------------------------------------

            var farms =
                _db.GetFarmsByUserId(userId)
                   ?.ToList();


            // -----------------------------------------------------
            // USER HAS NO FARMS
            // -----------------------------------------------------

            if (farms == null || farms.Count == 0)
            {
                return View("LocalWeather");
            }


            // -----------------------------------------------------
            // USER HAS ONE FARM
            // DIRECTLY OPEN FARM WEATHER
            // -----------------------------------------------------

            if (farms.Count == 1)
            {
                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        farmId = farms[0].Id
                    });
            }


            // -----------------------------------------------------
            // USER HAS MULTIPLE FARMS
            // SHOW FARM SELECTION
            // -----------------------------------------------------

            return View("SelectFarm", farms);
        }


        // =========================================================
        // LOCAL WEATHER PAGE
        // GET: /Weather/LocalWeather
        //
        // Used when:
        // - User is logged out
        // - User has no farms
        // - User wants to check weather near their location
        //
        // The browser obtains latitude/longitude using
        // navigator.geolocation().
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult LocalWeather()
        {
            return View();
        }


        // =========================================================
        // LOCAL WEATHER DATA
        // GET:
        // /Weather/LocalWeatherData?latitude=...&longitude=...
        //
        // This endpoint receives GPS coordinates from the browser
        // and gets live weather from Open-Meteo.
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> LocalWeatherData(
            double latitude,
            double longitude)
        {
            // -----------------------------------------------------
            // VALIDATE LATITUDE
            // -----------------------------------------------------

            if (latitude < -90 ||
                latitude > 90)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid latitude."
                });
            }


            // -----------------------------------------------------
            // VALIDATE LONGITUDE
            // -----------------------------------------------------

            if (longitude < -180 ||
                longitude > 180)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid longitude."
                });
            }


            // -----------------------------------------------------
            // GET WEATHER USING GPS COORDINATES
            // -----------------------------------------------------

            var weather =
                await _weatherService
                    .GetWeatherAsync(
                        latitude,
                        longitude);


            // -----------------------------------------------------
            // WEATHER FAILED
            // -----------------------------------------------------

            if (weather == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "We could not retrieve weather information for your current location right now."
                });
            }


            // -----------------------------------------------------
            // WEATHER SUCCESS
            // -----------------------------------------------------

            return Json(new
            {
                success = true,
                weather
            });
        }


        // =========================================================
        // FARM WEATHER
        // GET: /Weather/Index?farmId=3&cropId=2
        //
        // This displays the existing farm weather page.
        // =========================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index(
            int? farmId,
            int? cropId)
        {
            // -----------------------------------------------------
            // GET CURRENT USER ID
            // -----------------------------------------------------

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            // -----------------------------------------------------
            // IF NO FARM ID WAS PROVIDED
            // USE THE USER'S FIRST FARM
            //
            // Normally Select() handles this.
            // This also keeps direct /Weather/Index links working.
            // -----------------------------------------------------

            if (!farmId.HasValue)
            {
                var farms =
                    _db.GetFarmsByUserId(userId)
                       ?.ToList();

                var firstFarm =
                    farms?.FirstOrDefault();

                if (firstFarm == null)
                {
                    return RedirectToAction(
                        nameof(Select));
                }

                farmId = firstFarm.Id;
            }


            // -----------------------------------------------------
            // GET FARM
            // -----------------------------------------------------

            var farm =
                _db.GetFarm(
                    farmId.Value,
                    userId);

            if (farm == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // GET ALL CROPS SAVED FOR THIS FARM
            // -----------------------------------------------------

            var farmCrops =
                _db.GetFarmCrops(
                    farmId.Value,
                    userId)
                ?.ToList();


            // -----------------------------------------------------
            // SELECTED CROP
            // -----------------------------------------------------

            FarmCrop? selectedCrop = null;

            if (cropId.HasValue)
            {
                selectedCrop =
                    farmCrops?
                        .FirstOrDefault(
                            c => c.Id == cropId.Value);

                if (selectedCrop == null)
                {
                    return NotFound();
                }
            }


            // -----------------------------------------------------
            // CROP NAME
            //
            // Priority:
            //
            // 1. Explicitly selected crop
            // 2. Crop stored directly on FarmProfile
            // 3. First crop saved in FarmCrop table
            // -----------------------------------------------------

            var cropName =
                selectedCrop?.CropName;

            if (string.IsNullOrWhiteSpace(cropName))
            {
                cropName =
                    farm.Crop;
            }

            if (string.IsNullOrWhiteSpace(cropName))
            {
                cropName =
                    farmCrops?
                        .FirstOrDefault()?
                        .CropName;
            }

            cropName ??= "";


            // -----------------------------------------------------
            // CREATE WEATHER VIEW MODEL
            // -----------------------------------------------------

            var model = new WeatherViewModel
            {
                Farm = farm,

                CropKnowledge =
                    _cropKnowledge.GetCrop(
                        cropName)
            };


            // -----------------------------------------------------
            // CHECK FARM LOCATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    farm.Location))
            {
                model.ErrorMessage =
                    "Farm location is not available. Please update your farm profile.";

                return View(model);
            }


            // -----------------------------------------------------
            // GET FARM WEATHER
            // -----------------------------------------------------

            var weather =
                await _weatherService
                    .GetWeatherAsync(
                        farm.Location);


            // -----------------------------------------------------
            // WEATHER FAILED
            // -----------------------------------------------------

            if (weather == null)
            {
                model.ErrorMessage =
                    "We could not retrieve weather information for this location right now. Please try again.";

                return View(model);
            }


            // -----------------------------------------------------
            // WEATHER SUCCESS
            // -----------------------------------------------------

            model.Weather =
                weather;

            model.HasWeather =
                true;


            // -----------------------------------------------------
            // VIEW BAG
            // -----------------------------------------------------

            ViewBag.CropId =
                selectedCrop?.Id;

            ViewBag.CropName =
                cropName;


            // -----------------------------------------------------
            // DISPLAY EXISTING WEATHER PAGE
            // -----------------------------------------------------

            return View(model);
        }
    }
}