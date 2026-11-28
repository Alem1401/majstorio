import 'package:majstorio_desktop/models/offer/offer.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';

// Read-only list of all offers for the administrator.
class OfferProvider extends BaseProvider<Offer> {
  OfferProvider() : super("Offers", Offer.fromJson);
}
