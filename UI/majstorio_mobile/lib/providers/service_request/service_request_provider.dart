import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/models/service_request/service_request_search_request.dart';
import 'package:majstorio_mobile/models/service_request/service_request_upsert_request.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// Service requests. The API decides which requests the user sees:
// a customer sees their own, a tradesperson sees open requests in their city and categories
// plus the requests they own or have offered on.
class ServiceRequestProvider extends BaseProvider<ServiceRequest> {
  ServiceRequestProvider() : super("ServiceRequests", ServiceRequest.fromJson);

  // The request with its offers, appointment and status history.
  Future<ServiceRequestDetail> getById(int id) async {
    return ServiceRequestDetail.fromJson(await getJson("ServiceRequests/$id"));
  }

  // The newest request that is still running (Created, Offer accepted or Appointment confirmed),
  // or null when there is none. One small read per status, then the newest of the three wins.
  Future<ServiceRequest?> getLatestActive() async {
    var pages = await Future.wait([
      for (var status in [ServiceRequest.created, ServiceRequest.offerAccepted, ServiceRequest.appointmentConfirmed])
        get(ServiceRequestSearchRequest(status: status, page: 1, pageSize: 1)),
    ]);
    ServiceRequest? newest;
    for (var page in pages) {
      for (var request in page.items) {
        if (newest == null ||
            request.createdAtUtc.isAfter(newest.createdAtUtc) ||
            (request.createdAtUtc == newest.createdAtUtc && request.id > newest.id)) {
          newest = request;
        }
      }
    }
    return newest;
  }

  // The API answers with an empty body, so the new request's id is not known here;
  // the screen reloads the list, where the new request is at the top.
  Future<void> insert(ServiceRequestUpsertRequest request) async {
    await command("POST", "ServiceRequests", request.toJson());
  }

  // Only while the request is Created and has no offers. The chosen tradesperson cannot be changed.
  Future<void> update(int id, ServiceRequestUpsertRequest request) async {
    var body = request.toJson()..remove("targetTradespersonProfileId");
    await command("PUT", "ServiceRequests/$id", body);
  }

  // Only while the request is Created and has no offers. Afterwards it can only be cancelled.
  Future<void> delete(int id) async {
    await command("DELETE", "ServiceRequests/$id");
  }

  // The API cancels the request, its appointment and its open offers, and writes the status history.
  Future<void> cancel(int id, String? reason) async {
    await command("POST", "ServiceRequests/$id/cancel", {
      if (reason != null && reason.isNotEmpty) "reason": reason,
    });
  }
}
