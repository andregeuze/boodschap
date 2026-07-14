using Boodschap.Features.Nutrition.Application;
using Boodschap.Features.Nutrition.Domain;

namespace Boodschap.Features.Nutrition.Tests;

public sealed class FoodServiceTests
{
	[Fact]
	public async Task ImportNevoDetailsAsync_ReadsFoodsAndUpsertsThem()
	{
		var importedFoods = new List<Food>
		{
			new()
			{
				NevoCode = "1",
				Name = "Aardappelen rauw"
			},
			new()
			{
				NevoCode = "2",
				Name = "Banaan"
			}
		};
		var repository = new FakeFoodRepository();
		var importer = new FakeNevoFoodImporter(importedFoods);
		var service = new FoodService(repository, importer);

		await using var stream = new MemoryStream([1, 2, 3]);
		var result = await service.ImportNevoDetailsAsync(stream);

		Assert.NotSame(stream, importer.LastSource);
		Assert.Equal(new byte[] { 1, 2, 3 }, importer.LastSourceBytes);
		Assert.Equal(2, result.ImportedFoods);
		Assert.Same(importedFoods, repository.LastUpsertedFoods);
	}

	[Fact]
	public async Task ImportNevoDetailsAsync_BuffersStreamsThatDoNotSupportSynchronousReads()
	{
		var importedFoods = new List<Food>
		{
			new()
			{
				NevoCode = "1",
				Name = "Aardappelen rauw"
			}
		};
		var repository = new FakeFoodRepository();
		var importer = new FakeNevoFoodImporter(importedFoods);
		var service = new FoodService(repository, importer);

		await using var stream = new AsyncOnlyReadStream([1, 2, 3]);
		var result = await service.ImportNevoDetailsAsync(stream);

		Assert.Equal(new byte[] { 1, 2, 3 }, importer.LastSourceBytes);
		Assert.Equal(1, result.ImportedFoods);
		Assert.Same(importedFoods, repository.LastUpsertedFoods);
	}

	private sealed class FakeFoodRepository : IFoodRepository
	{
		public IReadOnlyCollection<Food>? LastUpsertedFoods { get; private set; }

		public Task<IReadOnlyList<Food>> GetFoodsAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IReadOnlyList<Food>>([]);
		}

		public Task<IReadOnlyList<Food>> SearchFoodsAsync(string query, CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IReadOnlyList<Food>>([]);
		}

		public Task UpsertFoodsAsync(IReadOnlyCollection<Food> foods, CancellationToken cancellationToken = default)
		{
			LastUpsertedFoods = foods;
			return Task.CompletedTask;
		}
	}

	private sealed class FakeNevoFoodImporter(IReadOnlyList<Food> foods) : INevoFoodImporter
	{
		public Stream? LastSource { get; private set; }
		public byte[]? LastSourceBytes { get; private set; }

		public IReadOnlyList<Food> ReadFoods(Stream source)
		{
			LastSource = source;
			using var buffer = new MemoryStream();
			source.CopyTo(buffer);
			LastSourceBytes = buffer.ToArray();
			return foods;
		}
	}

	private sealed class AsyncOnlyReadStream(byte[] content) : Stream
	{
		private int position;

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException("Synchronous reads are not supported.");
		}

		public override int Read(Span<byte> buffer)
		{
			throw new NotSupportedException("Synchronous reads are not supported.");
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return Task.FromResult(ReadInto(buffer.AsMemory(offset, count)));
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			return ValueTask.FromResult(ReadInto(buffer));
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		private int ReadInto(Memory<byte> buffer)
		{
			var availableBytes = Math.Min(buffer.Length, content.Length - position);
			if (availableBytes <= 0)
			{
				return 0;
			}

			content.AsMemory(position, availableBytes).CopyTo(buffer);
			position += availableBytes;
			return availableBytes;
		}
	}
}