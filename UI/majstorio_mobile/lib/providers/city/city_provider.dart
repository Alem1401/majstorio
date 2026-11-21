import 'package:majstorio_mobile/models/city/city.dart';
import 'package:majstorio_mobile/models/city/city_search_request.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// Cities for the pickers. The mobile app only reads them; administrators change them on the desktop.
class CityProvider extends BaseProvider<City> {
  CityProvider() : super("Cities", City.fromJson);

  // All active cities, sorted by name, read page by page (the server sends at most 100 per page).
  Future<List<City>> getActive() async {
    var cities = <City>[];
    var page = 1;
    while (true) {
      var result = await get(CitySearchRequest(page: page, pageSize: 100));
      cities.addAll(result.items);
      if (result.items.isEmpty || cities.length >= result.totalCount) break;
      page++;
    }
    cities.sort((a, b) => a.name.compareTo(b.name));
    return cities;
  }
}
