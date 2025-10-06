using Microsoft.AspNetCore.Mvc;

namespace SOP.Controllers
{
    [Route("/api/[Controller]")]
    [ApiController]
    public class PresetController : ControllerBase
    {
        private readonly IPresetRepository _presetRepository;


        public PresetController(IPresetRepository presetRepository)
        {

            _presetRepository = presetRepository;

        }

        //[Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var presets = await _presetRepository.GetAllAsync();

                List<PresetResponse> presetResponses = presets.Select(
                    preset => MapPresetToPresetResponse(preset)).ToList();

                return Ok(presetResponses);

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


        //[Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] PresetRequest presetRequest)
        {
            try
            {
                Preset newPreset = MapPresetRequestToPreset(presetRequest);

                var preset = await _presetRepository.CreateAsync(newPreset);

                PresetResponse presetResponse = MapPresetToPresetResponse(preset);

                return Ok(preset);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


        //[Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        [Route("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            try
            {
                var preset = await _presetRepository.FindByIdAsync(Id);
                if (preset == null)
                {
                    return NotFound();
                }

                return Ok(MapPresetToPresetResponse(preset));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


        //[Authorize("Admin")]
        [HttpDelete]
        [Route("{presetId}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int presetId)
        {
            try
            {
                var preset = await _presetRepository.DeleteByIdAsync(presetId);
                if (preset == null)
                {
                    return NotFound(); //Status Code 404
                }

                return Ok(MapPresetToPresetResponse(preset));
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
        }



        [NonAction]

        public PresetResponse MapPresetToPresetResponse(Preset preset) {

            return new PresetResponse
            {
                Id = preset.Id,
                Name = preset.Name,
                Data = preset.Data,

            };
        }

        [NonAction]
        private Preset MapPresetRequestToPreset(PresetRequest presetRequest)
        {
            return new Preset
            {
                Name = presetRequest.Name,
                Data = presetRequest.Data,
            };
        }


    }
}
