namespace SOP.Repositories
{
    public interface IPresetRepository
    {
        Task<Preset> CreateAsync(Preset preset);
        Task<Preset?> FindByIdAsync(int presetId);
        Task<List<Preset>> GetAllAsync();

        Task<Preset?> DeleteByIdAsync(int presetId);

    }


    public class PresetRepository : IPresetRepository
    {
        private readonly DatabaseContext _context;


        public PresetRepository(DatabaseContext context)
        {
            _context = context;
        }


        public async Task<Preset> CreateAsync(Preset newPreset)
        {
            _context.Presets.Add(newPreset);
            await _context.SaveChangesAsync();
            newPreset = await FindByIdAsync(newPreset.Id);
            return newPreset;
        }

        public async Task<Preset?> FindByIdAsync(int presetId)
        {
            return await _context.Presets.FindAsync(presetId);
        }


        public async Task<List<Preset>> GetAllAsync() {
        
            return await _context.Presets.ToListAsync();
        
        }

        public async Task<Preset?> DeleteByIdAsync(int presetId)
        {
            var preset = await FindByIdAsync(presetId);
            if (preset != null)
            {
                _context.Presets.Remove(preset);
                await _context.SaveChangesAsync();
            }

            return preset;
        }
    }

}
