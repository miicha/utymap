using System.Text;
using UtyDepend;
using UtyMap.Unity.Geocoding;
using UtyRx;

namespace Assets.Scripts.Debug.Commands
{
    /// <summary> Represents reverse geocoding command. </summary>
    internal class GeocodeCommand : ICommand
    {
        private readonly IGeocoder _geoCoder;

        /// <inheritdoc />
        public string Name { get { return "geocode"; } }

        /// <inheritdoc />
        public string Description { get { return ""; } }

        /// <summary> Creates instance of <see cref="GeocodeCommand"/>. </summary>
        /// <param name="geoCoder">Geocoder.</param>
        [Dependency]
        public GeocodeCommand(IGeocoder geoCoder)
        {
            _geoCoder = geoCoder;
        }

        /// <inheritdoc />
        public IObservable<string> Execute(params string[] args)
        {
            return Observable.Create<string>(o =>
            {
                var response = new StringBuilder();
                if (ShouldPrintHelp(args))
                {
                    PrintHelp(response);
                    o.OnNext(response.ToString());
                    o.OnCompleted();
                } else
                {
                    _geoCoder.Search(args[1]).Subscribe(r =>
                        response.AppendFormat("{0} {1}\n", r.Coordinate, r.DisplayName),
                        () =>
                        {
                            o.OnNext(response.ToString());
                            o.OnCompleted();
                        });
                }
                return Disposable.Empty;
            });
        }

        private bool ShouldPrintHelp(string[] args)
        {
            return args.Length != 2;
        }

        private void PrintHelp(StringBuilder response)
        {
            response.AppendLine("Usage: geocode <place name>");
        }
    }
}
