using System.Collections.Generic;
using System.Linq;
using Bogus;
using MusicGen.Core.Common;
using MusicGen.Core.Theory;

namespace MusicGen.Core
{
    public class MusicDataGenerator
    {
        public MusicDataDto GenerateOne(long seed, string language = "en")
        {
            int seed32 = (int)(seed ^ (seed >> 32));
            var random = new MusicGen.Core.Common.RandomProvider(seed32);

            int genreIndex = random.Genre(4);
            var genreProfile = MusicGen.Core.Theory.GenreProfiles.GetProfile(genreIndex);

            var itemFaker = new Faker<MusicDataDto>(language ?? "en");
            itemFaker.UseSeed(seed32);

            itemFaker
                .RuleFor(m => m.ArtistName, f => f.Company.CompanyName())
                .RuleFor(
                    m => m.MusicName,
                    f => f.Commerce.ProductAdjective() + " " + f.Commerce.Product()
                )
                .RuleFor(m => m.Likes, f => f.Random.Int(1, 10))
                .RuleFor(
                    m => m.AlbumTitle,
                    f => f.Address.StreetName() + " " + f.Random.Number(1995, 2026)
                );

            var dto = itemFaker.Generate();
            dto.Genre = genreProfile.Name;
            dto.Seed = seed.ToString();
            return dto;
        }

        public List<MusicDataDto> Generate(
            long seed,
            int skip,
            int take,
            string language = "en",
            double likeFilter = -1
        )
        {
            var results = new List<MusicDataDto>();
            int found = 0;
            int currentSkip = 0;
            int i = 0;
            int safetyCounter = 0;
            const int MaxIterations = 10000;

            int masterSeed = (int)(seed ^ (seed >> 32));
            var masterRng = new Random(masterSeed);

            while (found < take && safetyCounter < MaxIterations)
            {
                long itemSeed = masterRng.NextInt64();

                var dto = GenerateOne(itemSeed, language);
                var random = new Random((int)(itemSeed ^ (itemSeed >> 32)));
                bool keep = false;

                if (likeFilter < 0)
                {
                    keep = true;
                }
                else
                {
                    if (dto.Likes == Math.Floor(likeFilter))
                    {
                        if (random.NextDouble() >= likeFilter - Math.Floor(likeFilter))
                        {
                            keep = true;
                        }
                    }
                    else if (dto.Likes == Math.Ceiling(likeFilter))
                    {
                        if (random.NextDouble() <= likeFilter - Math.Floor(likeFilter))
                        {
                            keep = true;
                        }
                    }
                }

                if (keep)
                {
                    if (currentSkip < skip)
                    {
                        currentSkip++;
                    }
                    else
                    {
                        results.Add(dto);
                        found++;
                    }
                }
                i++;
                safetyCounter++;
            }
            return results;
        }
    }
}
