using System;
using System.Threading;

namespace AiStockAdvisor.Logging
{
    public static class LogScope
    {
        private sealed class ScopeState
        {
            public string? LogId { get; set; }
            public string? SpanId { get; set; }
            public string? ParentSpanId { get; set; }
        }

        private sealed class Scope : IDisposable
        {
            private readonly ScopeState? _prior;

            public Scope(ScopeState? prior)
            {
                _prior = prior;
            }

            public void Dispose()
            {
                CurrentState.Value = _prior;
            }
        }

        private sealed class EmptyScope : IDisposable
        {
            public static readonly EmptyScope Instance = new EmptyScope();

            public void Dispose()
            {
            }
        }

        private static readonly AsyncLocal<ScopeState?> CurrentState = new AsyncLocal<ScopeState?>();

        public static string? CurrentLogId => CurrentState.Value?.LogId;
        public static string? CurrentSpanId => CurrentState.Value?.SpanId;
        public static string? CurrentParentSpanId => CurrentState.Value?.ParentSpanId;

        public static IDisposable EnsureFlow()
        {
            if (CurrentState.Value == null || string.IsNullOrWhiteSpace(CurrentState.Value.LogId))
            {
                return BeginFlow();
            }

            return EmptyScope.Instance;
        }

        public static IDisposable BeginFlow(string? logId = null, string? spanId = null)
        {
            var next = new ScopeState
            {
                LogId = string.IsNullOrWhiteSpace(logId) ? Guid.NewGuid().ToString() : logId,
                SpanId = string.IsNullOrWhiteSpace(spanId) ? Guid.NewGuid().ToString() : spanId,
                ParentSpanId = null
            };

            return Push(next);
        }

        public static IDisposable BeginBranch(string? spanId = null)
        {
            var current = CurrentState.Value;
            var next = new ScopeState
            {
                LogId = current?.LogId ?? Guid.NewGuid().ToString(),
                SpanId = string.IsNullOrWhiteSpace(spanId) ? Guid.NewGuid().ToString() : spanId,
                ParentSpanId = current?.SpanId
            };

            return Push(next);
        }

        public static IDisposable Use(string? logId, string? spanId = null, string? parentSpanId = null)
        {
            if (string.IsNullOrWhiteSpace(logId) && string.IsNullOrWhiteSpace(spanId) && string.IsNullOrWhiteSpace(parentSpanId))
            {
                return EmptyScope.Instance;
            }

            var next = new ScopeState
            {
                LogId = logId,
                SpanId = spanId,
                ParentSpanId = parentSpanId
            };

            return Push(next);
        }

        public static string FormatMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            var state = CurrentState.Value;
            if (state == null || string.IsNullOrWhiteSpace(state.LogId))
            {
                return message;
            }

            var spanPart = string.IsNullOrWhiteSpace(state.SpanId) ? string.Empty : $" spanId={state.SpanId}";
            var parentPart = string.IsNullOrWhiteSpace(state.ParentSpanId) ? string.Empty : $" parentSpanId={state.ParentSpanId}";
            return $"[logId={state.LogId}{spanPart}{parentPart}] {message}";
        }

        private static IDisposable Push(ScopeState next)
        {
            var prior = CurrentState.Value;
            CurrentState.Value = next;
            return new Scope(prior);
        }
    }
}
