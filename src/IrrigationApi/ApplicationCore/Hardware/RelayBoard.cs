using System;
using System.Collections;
using System.Collections.Generic;
using System.Device.Gpio;

namespace IrrigationApi.ApplicationCore.Hardware
{
    /// <summary>
    /// A board containing some <see cref="Relay"/> devices.
    /// </summary>
    /// <example>
    /// Create a relay board, with a relay:
    /// <code>
    /// RelayBoard board = new RelayBoard();
    /// board.CreateRelay(pin: 1);
    /// </code>
    /// </example>
    public class RelayBoard : IDisposable, IEnumerable<Relay>, IReadOnlyCollection<Relay>, IReadOnlyDictionary<int, Relay>
    {
        private readonly GpioController _controller;
        private readonly Dictionary<int, Relay> _relays;

        /// <summary>
        /// Gets the type of relay board.
        /// </summary>
        public RelayType Type { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayBoard"/> class.
        /// </summary>
        public RelayBoard(RelayType relayType, GpioController gpioController, params int[] pins)
        {
            Type = relayType;
            _controller = gpioController;
            _relays = new();

            foreach (var pin in pins)
            {
                if (_relays.ContainsKey(pin))
                { throw new ArgumentException("Pin already in use.", nameof(pin)); }

                _relays.Add(pin, new Relay(pin, Type, _controller));
            }
        }       

        /// <summary>
        /// Get a relay based on the specified pin.
        /// </summary>
        /// <param name="pin">The pin of the relay.</param>
        /// <returns>The relay, or null if a relay does not exist on that pin.</returns>
        public Relay? GetRelay(int pin)
        {
            var result = _relays.TryGetValue(pin, out Relay? value);
            return result ? value : null;
        }

        /// <summary>
        /// Update a relays state.
        /// </summary>
        /// <param name="pin">Pin relay is on.</param>
        /// <param name="state">State of pin to set.</param>
        public void Set(int pin, bool state)
        {
            if (!_relays.ContainsKey(pin))
            {
                throw new ArgumentOutOfRangeException($"No relay exists on pin {pin}.", nameof(pin));
            }

            _relays[pin].On = state;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_relays != null)
            {
                foreach (var item in _relays.Values)
                {
                    item?.Dispose();
                }

                _relays.Clear();
            }
        }

        /// <inheritdoc/>
        public IEnumerable<int> Keys => _relays.Keys;

        /// <inheritdoc/>
        public IEnumerable<Relay> Values => _relays.Values;

        /// <inheritdoc/>
        public int Count => _relays.Count;

        /// <inheritdoc/>
        public Relay this[int key] => _relays[key];

        /// <inheritdoc/>
        public IEnumerator<Relay> GetEnumerator()
            => _relays.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _relays.Values.GetEnumerator();

        /// <inheritdoc/>
        public bool ContainsKey(int key) => _relays.ContainsKey(key);

        /// <inheritdoc/>
        public bool TryGetValue(int key, out Relay value)
            => _relays.TryGetValue(key, out value);

        IEnumerator<KeyValuePair<int, Relay>> IEnumerable<KeyValuePair<int, Relay>>.GetEnumerator() => _relays.GetEnumerator();
    }
}
