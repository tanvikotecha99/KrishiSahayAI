using KrishiSahayAI.Models;

namespace KrishiSahayAI.Services
{
    public interface ICropKnowledgeService
    {
        CropKnowledge? GetCrop(string cropName);
    }

    public class CropKnowledgeService : ICropKnowledgeService
    {
        private readonly List<CropKnowledge> _crops;

        public CropKnowledgeService()
        {
            _crops = new List<CropKnowledge>
            {
                new CropKnowledge
                {
                    CropName = "Groundnut",

                    Overview =
                        "Groundnut is a legume crop that develops pods underground. Successful cultivation depends on suitable soil conditions, adequate moisture, appropriate temperature and careful crop management.",

                    SoilRequirements =
                        "Groundnut generally performs well in well-drained soils that allow pods to develop and be harvested more easily. Avoid prolonged waterlogging. Soil testing is useful for understanding pH and nutrient availability.",

                    WaterRequirements =
                        "Groundnut needs adequate moisture during establishment and important reproductive stages. Water management should respond to soil moisture, crop stage and weather. Avoid prolonged waterlogging.",

                    ClimateRequirements =
                        "Groundnut is generally suited to warm growing conditions. Crop performance depends on local temperature, rainfall pattern and the selected variety.",

                    PlantingGuidance =
                        "Use healthy seed of a locally suitable variety. Follow locally recommended planting time, spacing and seed-treatment practices for the selected variety and production system.",

                    GrowthGuidance =
                        "Monitor plant establishment, weed pressure, pests, diseases, moisture and crop development. Management should be adjusted according to crop stage and local recommendations.",

                    HarvestGuidance =
                        "Harvest timing should be based on crop maturity indicators appropriate for the variety and production system. Avoid harvesting too early or leaving mature pods in the field unnecessarily.",

                    ImportantNote =
                        "Groundnut recommendations can vary by variety, soil, season and local conditions. Use local agricultural guidance and soil-test results before making major input decisions."
                },

                new CropKnowledge
                {
                    CropName = "Wheat",

                    Overview =
                        "Wheat is a cereal crop commonly grown during cooler growing seasons. Crop performance depends on suitable temperature, soil, water and timely management.",

                    SoilRequirements =
                        "Wheat can grow in several soil types when drainage, fertility and soil structure are suitable. Soil testing can help guide nutrient management.",

                    WaterRequirements =
                        "Water requirements vary with soil, weather, variety and crop stage. Adequate moisture during important development stages is important.",

                    ClimateRequirements =
                        "Wheat generally performs best under suitable cool-season conditions, with crop-specific temperature requirements changing through development.",

                    PlantingGuidance =
                        "Use quality seed of a locally recommended variety and follow local recommendations for sowing date, seed rate, spacing and depth.",

                    GrowthGuidance =
                        "Monitor crop establishment, weeds, pests, diseases, moisture and nutrient status throughout the season.",

                    HarvestGuidance =
                        "Harvest when the crop reaches appropriate maturity and grain moisture conditions according to local recommendations.",

                    ImportantNote =
                        "Wheat recommendations vary by variety, region, season and irrigation conditions."
                },

                new CropKnowledge
                {
                    CropName = "Cotton",

                    Overview =
                        "Cotton is a fibre crop requiring suitable temperature, soil, water and a sufficiently long growing period.",

                    SoilRequirements =
                        "Cotton can be grown in different soils depending on local conditions. Good drainage and suitable soil structure are important.",

                    WaterRequirements =
                        "Water needs vary by variety, soil, climate and growth stage. Both moisture stress and excessive moisture can affect crop performance.",

                    ClimateRequirements =
                        "Cotton requires warm growing conditions and is sensitive to unsuitable temperature and moisture conditions.",

                    PlantingGuidance =
                        "Select locally suitable varieties and follow recommended planting dates, spacing and seed practices.",

                    GrowthGuidance =
                        "Regularly monitor plant growth, weeds, insect pressure, diseases and moisture.",

                    HarvestGuidance =
                        "Harvest should follow local maturity and boll-opening guidance. Proper picking and handling help maintain fibre quality.",

                    ImportantNote =
                        "Cotton management is highly dependent on local climate, variety and pest-management recommendations."
                },

                new CropKnowledge
                {
                    CropName = "Rice",

                    Overview =
                        "Rice is a cereal crop that can be produced under different water and cultivation systems.",

                    SoilRequirements =
                        "Suitable soil depends on the rice production system. Soil testing can help understand fertility and pH.",

                    WaterRequirements =
                        "Water management is an important part of rice production. Requirements depend on cultivation system, rainfall, soil and crop stage.",

                    ClimateRequirements =
                        "Rice generally requires warm growing conditions, with specific requirements depending on variety and production system.",

                    PlantingGuidance =
                        "Follow locally recommended nursery, transplanting or direct-seeding practices depending on the selected system.",

                    GrowthGuidance =
                        "Monitor water, weeds, nutrients, pests and diseases throughout crop development.",

                    HarvestGuidance =
                        "Harvest timing should follow local maturity indicators and suitable grain moisture conditions.",

                    ImportantNote =
                        "Rice water management varies substantially between production systems and locations."
                },

                new CropKnowledge
                {
                    CropName = "Tomato",

                    Overview =
                        "Tomato is a vegetable crop that can be grown in open fields or protected systems.",

                    SoilRequirements =
                        "Tomato generally benefits from fertile, well-drained soil with suitable pH and organic matter.",

                    WaterRequirements =
                        "Tomato requires consistent moisture, particularly during flowering and fruit development. Avoid large fluctuations in moisture where possible.",

                    ClimateRequirements =
                        "Tomato growth is influenced strongly by temperature. Excessive heat or unsuitable cold can affect flowering and fruit development.",

                    PlantingGuidance =
                        "Use healthy seedlings or suitable seed and follow locally recommended spacing and planting practices.",

                    GrowthGuidance =
                        "Monitor plant growth, moisture, nutrients, pests and diseases. Support and pruning requirements depend on the variety and production system.",

                    HarvestGuidance =
                        "Harvest stage depends on intended market and transport requirements. Handle fruit carefully to reduce damage.",

                    ImportantNote =
                        "Tomato management varies with variety, season and whether the crop is grown in open field or protected conditions."
                }
            };
        }

        public CropKnowledge? GetCrop(string cropName)
        {
            if (string.IsNullOrWhiteSpace(cropName))
            {
                return null;
            }

            return _crops.FirstOrDefault(
                x => string.Equals(
                    x.CropName,
                    cropName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}