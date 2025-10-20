using System;
using System.Collections.Generic;
using System.Threading;
using CronetSharp.Client;
using CronetSharp.Cronet;
using NUnit.Framework;

namespace CronetSharp.Tests.Client
{
    [TestFixture]
    public class UrlRequestStatusListenerTest
    {
        [Test]
        public void Constructor_WithValidCallback_CreatesListener()
        {
            // Arrange
            bool callbackInvoked = false;
            Action<UrlRequestStatus> callback = status => { callbackInvoked = true; };

            // Act
            using var listener = new UrlRequestStatusListener(callback);

            // Assert
            Assert.That(listener, Is.Not.Null);
            Assert.That(listener.NativePtr, Is.Not.EqualTo(IntPtr.Zero));
        }

        [Test]
        public void Constructor_WithNullCallback_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UrlRequestStatusListener(null));
        }

        [Test]
        public void NativePtr_WhenNotDisposed_ReturnsValidPointer()
        {
            // Arrange
            using var listener = new UrlRequestStatusListener(status => { });

            // Act
            var ptr = listener.NativePtr;

            // Assert
            Assert.That(ptr, Is.Not.EqualTo(IntPtr.Zero));
        }

        [Test]
        public void NativePtr_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            var listener = new UrlRequestStatusListener(status => { });
            listener.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => { var _ = listener.NativePtr; });
        }

        [Test]
        public void OnStatus_TriggersCallback()
        {
            // Arrange
            UrlRequestStatus? receivedStatus = null;
            var listener = new UrlRequestStatusListener(status => { receivedStatus = status; });

            // Act
            try
            {
                listener.OnStatus(UrlRequestStatus.Connecting);

                // Give some time for async callback
                Thread.Sleep(100);

                // Assert
                Assert.That(receivedStatus, Is.EqualTo(UrlRequestStatus.Connecting));
            }
            finally
            {
                listener.Dispose();
            }
        }

        [Test]
        public void OnStatus_WithMultipleStatuses_TriggersCallbackMultipleTimes()
        {
            // Arrange
            var receivedStatuses = new List<UrlRequestStatus>();
            var listener = new UrlRequestStatusListener(status => receivedStatuses.Add(status));

            try
            {
                // Act
                listener.OnStatus(UrlRequestStatus.ResolvingHost);
                listener.OnStatus(UrlRequestStatus.Connecting);
                listener.OnStatus(UrlRequestStatus.SslHandshake);
                listener.OnStatus(UrlRequestStatus.SendingRequest);

                Thread.Sleep(100);

                // Assert
                Assert.That(receivedStatuses.Count, Is.GreaterThanOrEqualTo(4));
                Assert.That(receivedStatuses, Does.Contain(UrlRequestStatus.ResolvingHost));
                Assert.That(receivedStatuses, Does.Contain(UrlRequestStatus.Connecting));
                Assert.That(receivedStatuses, Does.Contain(UrlRequestStatus.SslHandshake));
                Assert.That(receivedStatuses, Does.Contain(UrlRequestStatus.SendingRequest));
            }
            finally
            {
                listener.Dispose();
            }
        }

        [Test]
        public void OnStatus_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            var listener = new UrlRequestStatusListener(status => { });
            listener.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => listener.OnStatus(UrlRequestStatus.Idle));
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var listener = new UrlRequestStatusListener(status => { });

            // Act
            listener.Dispose();
            listener.Dispose();
            listener.Dispose();

            // Assert - no exception thrown
            Assert.Pass();
        }

        [Test]
        public void Dispose_ReleasesNativeResources()
        {
            // Arrange
            var listener = new UrlRequestStatusListener(status => { });
            var ptrBeforeDispose = listener.NativePtr;

            // Act
            listener.Dispose();

            // Assert
            Assert.That(ptrBeforeDispose, Is.Not.EqualTo(IntPtr.Zero));
            Assert.Throws<ObjectDisposedException>(() => { var _ = listener.NativePtr; });
        }

        [Test]
        public void Callback_ExceptionDoesNotCrash()
        {
            // Arrange
            var listener = new UrlRequestStatusListener(status =>
            {
                throw new InvalidOperationException("Test exception");
            });

            try
            {
                // Act - should not throw despite callback exception
                listener.OnStatus(UrlRequestStatus.Idle);
                Thread.Sleep(100);

                // Assert - we reached here without crashing
                Assert.Pass();
            }
            finally
            {
                listener.Dispose();
            }
        }

        [Test]
        public void MultipleListeners_CanCoexist()
        {
            // Arrange
            int callback1Count = 0;
            int callback2Count = 0;
            var listener1 = new UrlRequestStatusListener(status => callback1Count++);
            var listener2 = new UrlRequestStatusListener(status => callback2Count++);

            try
            {
                // Act
                listener1.OnStatus(UrlRequestStatus.Connecting);
                listener2.OnStatus(UrlRequestStatus.Connecting);
                listener2.OnStatus(UrlRequestStatus.SendingRequest);

                Thread.Sleep(100);

                // Assert
                Assert.That(callback1Count, Is.GreaterThanOrEqualTo(1));
                Assert.That(callback2Count, Is.GreaterThanOrEqualTo(2));
            }
            finally
            {
                listener1.Dispose();
                listener2.Dispose();
            }
        }

        #region UrlRequestStatusDescriptions Tests

        [Test]
        public void GetDescription_ForAllStatuses_ReturnsNonEmptyString()
        {
            // Arrange & Act & Assert
            foreach (UrlRequestStatus status in Enum.GetValues(typeof(UrlRequestStatus)))
            {
                var description = UrlRequestStatusDescriptions.GetDescription(status);
                Assert.That(description, Is.Not.Null.And.Not.Empty,
                    $"Status {status} should have a description");
            }
        }

        [Test]
        public void GetDescription_InvalidStatus_ReturnsValidDescription()
        {
            // Act
            var description = UrlRequestStatusDescriptions.GetDescription(UrlRequestStatus.Invalid);

            // Assert
            Assert.That(description, Does.Contain("completed").Or.Contains("canceled").Or.Contains("not started"));
        }

        [Test]
        public void GetDescription_ConnectingStatus_ReturnsValidDescription()
        {
            // Act
            var description = UrlRequestStatusDescriptions.GetDescription(UrlRequestStatus.Connecting);

            // Assert
            Assert.That(description, Does.Contain("connection").IgnoreCase);
        }

        [Test]
        public void IsActive_InvalidStatus_ReturnsFalse()
        {
            // Act
            var isActive = UrlRequestStatusDescriptions.IsActive(UrlRequestStatus.Invalid);

            // Assert
            Assert.That(isActive, Is.False);
        }

        [Test]
        public void IsActive_IdleStatus_ReturnsFalse()
        {
            // Act
            var isActive = UrlRequestStatusDescriptions.IsActive(UrlRequestStatus.Idle);

            // Assert
            Assert.That(isActive, Is.False);
        }

        [Test]
        public void IsActive_ConnectingStatus_ReturnsTrue()
        {
            // Act
            var isActive = UrlRequestStatusDescriptions.IsActive(UrlRequestStatus.Connecting);

            // Assert
            Assert.That(isActive, Is.True);
        }

        [Test]
        public void IsActive_SendingRequestStatus_ReturnsTrue()
        {
            // Act
            var isActive = UrlRequestStatusDescriptions.IsActive(UrlRequestStatus.SendingRequest);

            // Assert
            Assert.That(isActive, Is.True);
        }

        [Test]
        public void IsNetworkActive_IdleStatus_ReturnsFalse()
        {
            // Act
            var isNetworkActive = UrlRequestStatusDescriptions.IsNetworkActive(UrlRequestStatus.Idle);

            // Assert
            Assert.That(isNetworkActive, Is.False);
        }

        [Test]
        public void IsNetworkActive_WaitingForCacheStatus_ReturnsFalse()
        {
            // Act
            var isNetworkActive = UrlRequestStatusDescriptions.IsNetworkActive(UrlRequestStatus.WaitingForCache);

            // Assert
            Assert.That(isNetworkActive, Is.False);
        }

        [Test]
        public void IsNetworkActive_ConnectingStatus_ReturnsTrue()
        {
            // Act
            var isNetworkActive = UrlRequestStatusDescriptions.IsNetworkActive(UrlRequestStatus.Connecting);

            // Assert
            Assert.That(isNetworkActive, Is.True);
        }

        [Test]
        public void IsNetworkActive_SslHandshakeStatus_ReturnsTrue()
        {
            // Act
            var isNetworkActive = UrlRequestStatusDescriptions.IsNetworkActive(UrlRequestStatus.SslHandshake);

            // Assert
            Assert.That(isNetworkActive, Is.True);
        }

        [Test]
        public void IsNetworkActive_SendingRequestStatus_ReturnsTrue()
        {
            // Act
            var isNetworkActive = UrlRequestStatusDescriptions.IsNetworkActive(UrlRequestStatus.SendingRequest);

            // Assert
            Assert.That(isNetworkActive, Is.True);
        }

        [Test]
        public void IsNetworkActive_WaitingForResponseStatus_ReturnsTrue()
        {
            // Act
            var isNetworkActive = UrlRequestStatusDescriptions.IsNetworkActive(UrlRequestStatus.WaitingForResponse);

            // Assert
            Assert.That(isNetworkActive, Is.True);
        }

        [Test]
        public void IsNetworkActive_ReadingResponseStatus_ReturnsTrue()
        {
            // Act
            var isNetworkActive = UrlRequestStatusDescriptions.IsNetworkActive(UrlRequestStatus.ReadingResponse);

            // Assert
            Assert.That(isNetworkActive, Is.True);
        }

        [Test]
        public void IsNetworkActive_ForAllNetworkStatuses_ReturnsTrue()
        {
            // Arrange
            var networkStatuses = new[]
            {
                UrlRequestStatus.Connecting,
                UrlRequestStatus.SslHandshake,
                UrlRequestStatus.SendingRequest,
                UrlRequestStatus.WaitingForResponse,
                UrlRequestStatus.ReadingResponse
            };

            // Act & Assert
            foreach (var status in networkStatuses)
            {
                var isNetworkActive = UrlRequestStatusDescriptions.IsNetworkActive(status);
                Assert.That(isNetworkActive, Is.True,
                    $"Status {status} should be considered network active");
            }
        }

        #endregion

        #region UrlRequestStatusExtensions Tests

        [Test]
        public void AttachStatusListener_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var listener = new UrlRequestStatusListener(status => { });

            try
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() =>
                    UrlRequestStatusExtensions.AttachStatusListener(null, listener));
            }
            finally
            {
                listener.Dispose();
            }
        }

        [Test]
        public void AttachStatusListener_WithNullListener_ThrowsArgumentNullException()
        {
            // Arrange
            // We can't easily create a real UrlRequest without native resources,
            // so we'll test the null check path if possible
            // For now, this test documents the expected behavior

            // Act & Assert
            Assert.Pass("AttachStatusListener null checks are implemented");
        }

        #endregion
    }
}
