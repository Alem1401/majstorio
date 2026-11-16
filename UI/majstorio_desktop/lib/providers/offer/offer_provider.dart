import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/offer/offer.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class OfferProvider extends BaseProvider<Offer> {
  OfferProvider() : super("Offers", Offer.fromJson);

  Future<int> delete(Offer offer) async {
    var response = await http.delete(Uri.parse("$baseUrl/Offers/${offer.id}"), headers: authHeaders());
    checkUnauthorized(response);
    return response.statusCode;
  }
}
