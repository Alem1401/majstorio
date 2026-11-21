import 'package:majstorio_mobile/models/offer/offer.dart';
import 'package:majstorio_mobile/models/offer/offer_search_request.dart';
import 'package:majstorio_mobile/models/offer/offer_upsert_request.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// Offers. The list is GET /Offers/mine: the offers a tradesperson sent, or the offers a customer received.
// (GET /Offers without "mine" is for administrators only.)
class OfferProvider extends BaseProvider<Offer> {
  OfferProvider() : super("Offers/mine", Offer.fromJson);

  // The signed-in tradesperson's offer on one request, or null when they have not sent one.
  // A tradesperson can send only one offer per request.
  Future<Offer?> getMineForRequest(int serviceRequestId) async {
    var page = await get(OfferSearchRequest(serviceRequestId: serviceRequestId, pageSize: 100));
    return page.items.isEmpty ? null : page.items.first;
  }

  // All of the user's offers, read page by page (the server sends at most 100 per page).
  // Used to join appointments to their offer: an appointment only knows its offer id,
  // and the offer knows the request and the agreed price.
  Future<List<Offer>> getAllMine() async {
    var offers = <Offer>[];
    var page = 1;
    while (true) {
      var result = await get(OfferSearchRequest(page: page, pageSize: 100));
      offers.addAll(result.items);
      if (result.items.isEmpty || offers.length >= result.totalCount) break;
      page++;
    }
    return offers;
  }

  // Tradesperson: a new offer on a request.
  Future<void> insert(OfferUpsertRequest request) async {
    await command("POST", "Offers", request.toJson());
  }

  // Tradesperson: change a pending offer.
  Future<void> update(int id, OfferUpsertRequest request) async {
    await command("PUT", "Offers/$id", request.toJson());
  }

  // Tradesperson: take back a pending offer. The offer stays, with the status Withdrawn.
  Future<void> withdraw(int id) async {
    await command("POST", "Offers/$id/withdraw");
  }

  // Customer: accept a pending offer. The API closes the other offers, creates and confirms the appointment,
  // and writes both status changes of the request.
  Future<void> accept(int id) async {
    await command("POST", "Offers/$id/accept");
  }
}
