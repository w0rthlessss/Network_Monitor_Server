using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Models.MainDBModels;
using Network_Monitor_API.Data;

namespace Network_Monitor_API.Services
{
    public class PredictionService
    {
        private readonly MainDBContext _context;
        public PredictionService(MainDBContext context)
        {
            _context = context;
        }
        public async Task<List<PredictionDTO>> GetAllPredictionsAsync()
        {
            return await _context.Predictions
                .OrderByDescending(c => c.Timestamp)
                .Select(c => new PredictionDTO
                {
                    Id = c.Id,
                    ConnectionId = c.ConnectionId,
                    Result = c.Result,
                    Confidence = c.Confidence,
                    TopFeature = c.TopFeature,
                    Timestamp = c.Timestamp
                })
                .ToListAsync<PredictionDTO>();
        }

        public async Task<List<PredictionDTO>> GetPredictionsByConnectionIdAsync(int connectionId)
        {
            return await _context.Predictions
                .Where(c => c.ConnectionId == connectionId)
                .OrderByDescending(c => c.Timestamp)
                .Select(c => new PredictionDTO
                {
                    Id = c.Id,
                    ConnectionId = c.ConnectionId,
                    Result = c.Result,
                    Confidence = c.Confidence,
                    TopFeature = c.TopFeature,
                    Timestamp = c.Timestamp
                })
                .ToListAsync<PredictionDTO>();
        }

        public async Task<PredictionDTO?> GetPredictionByIdAsync(int id)
        {
            return await _context.Predictions
                .Where(c => c.Id == id)
                .Select(c => new PredictionDTO
                {
                    Id = c.Id,
                    ConnectionId = c.ConnectionId,
                    Result = c.Result,
                    Confidence = c.Confidence,
                    TopFeature = c.TopFeature,
                    Timestamp = c.Timestamp
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CreateNewPredictionRecordAsync(Prediction _prediction)
        {
            if (_prediction == null) return false;

            await _context.Predictions.AddAsync(_prediction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePredictionRecordByIdAsync(int id, PredictionDTO upd)
        {
            if (upd == null) return false;

            var prediction = await _context.Predictions.FirstOrDefaultAsync(p => p.Id == id);
            if (prediction == null) return false;

            prediction.ConnectionId = upd.ConnectionId;
            prediction.Result = upd.Result;
            prediction.Confidence = upd.Confidence;
            prediction.TopFeature = upd.TopFeature;
            prediction.Timestamp = upd.Timestamp;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemovePredictionRecordByIdAsync(int id)
        {
            var prediction = await _context.Predictions.FirstOrDefaultAsync(p => p.Id == id);
            if (prediction == null) return false;

            _context.Predictions.Remove(prediction);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
