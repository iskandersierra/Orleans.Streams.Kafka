﻿using Orleans;
using Orleans.Providers;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace TestGrains
{
	[StorageProvider(ProviderName = "Default")]
	public class TestGrain : Grain<TestModel>, ITestGrain
	{
		public Task<string> GetThePhrase()
		{
			const string phrase = "First message from the TestGrain. Now write Something and see it sent through kafka to the grains. (Will be printed in the Silo console window ;))";
			return Task.FromResult(phrase);
		}

		public override async Task OnActivateAsync()
		{
			var kafkaProvider = GetStreamProvider("KafkaProvider");
			var testStream = kafkaProvider.GetStream<TestModel>("streamId", "sucrose-test"); // todo: use stream utils

			// To resume stream in case of stream deactivation
			var subscriptionHandles = await testStream.GetAllSubscriptionHandles();

			if (subscriptionHandles.Count > 0)
			{
				foreach (var subscriptionHandle in subscriptionHandles)
				{
					await subscriptionHandle.ResumeAsync(OnNextTestMessage);
				}
			}
			else
			{
				await testStream.SubscribeAsync(OnNextTestMessage);
			}
		}

		private Task OnNextTestMessage(TestModel message, StreamSequenceToken sequenceToken)
		{
			Console.BackgroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine(message.Greeting);
			return Task.CompletedTask;
		}
	}
}
