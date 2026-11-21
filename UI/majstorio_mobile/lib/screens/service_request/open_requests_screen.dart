import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/models/service_request/service_request_search_request.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/providers/service_request/service_request_provider.dart';
import 'package:majstorio_mobile/screens/appointment/appointments_screen.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_details_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:majstorio_mobile/widgets/pager.dart';
import 'package:provider/provider.dart';

// "Radni prostor" for a tradesperson: open requests (status Created) the API lets them see —
// requests in their city and categories, plus requests they own or already offered on.
// Each row says whether an offer was already sent. Opening a row shows the request with the offer actions.
class OpenRequestsScreen extends StatefulWidget {
  const OpenRequestsScreen({super.key});

  @override
  State<OpenRequestsScreen> createState() => _OpenRequestsScreenState();
}

class _OpenRequestsScreenState extends State<OpenRequestsScreen> {
  late ServiceRequestProvider _serviceRequestProvider;
  late OfferProvider _offerProvider;

  var requests = <ServiceRequest>[];
  // The requests this tradesperson already sent an offer for.
  var offeredRequestIds = <int>{};
  var isLoading = true;
  String? error;

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;
  var _loadNumber = 0;

  @override
  void initState() {
    _serviceRequestProvider = context.read<ServiceRequestProvider>();
    _offerProvider = context.read<OfferProvider>();
    _serviceRequestProvider.addListener(loadRequests);
    _offerProvider.addListener(loadRequests);
    loadRequests();
    super.initState();
  }

  @override
  void dispose() {
    _serviceRequestProvider.removeListener(loadRequests);
    _offerProvider.removeListener(loadRequests);
    super.dispose();
  }

  Future<void> loadRequests() async {
    var loadNumber = ++_loadNumber;
    setState(() {
      isLoading = true;
      error = null;
    });
    try {
      // Both calls run at the same time.
      var requestsCall = _serviceRequestProvider.get(
        ServiceRequestSearchRequest(page: page, pageSize: pageSize, status: ServiceRequest.created),
      );
      var offersCall = _offerProvider.getAllMine();
      var response = await requestsCall;
      var offers = await offersCall;
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        requests = response.items;
        totalCount = response.totalCount;
        offeredRequestIds = {for (var offer in offers) offer.serviceRequestId};
        isLoading = false;
      });
    } catch (e) {
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        error = exceptionText(e);
        isLoading = false;
      });
    }
  }

  Widget buildRequestCard(ServiceRequest request) {
    var isOwn = request.customerId == AuthSession.userId;
    var isOffered = offeredRequestIds.contains(request.id);

    return Card(
      child: InkWell(
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (context) => ServiceRequestDetailsScreen(requestId: request.id)),
          );
        },
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      request.serviceCategoryName,
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: AppTheme.ink),
                    ),
                  ),
                  if (isOwn)
                    const StatusChip("Vaš zahtjev")
                  else if (isOffered)
                    const StatusChip("Ponuda poslana")
                  else
                    const StatusChip("Nova", highlighted: true),
                ],
              ),
              const SizedBox(height: 6),
              Text(request.description, maxLines: 2, overflow: TextOverflow.ellipsis),
              const SizedBox(height: 8),
              Wrap(
                spacing: 16,
                runSpacing: 4,
                children: [
                  IconText(Icons.person_outline, request.customerName),
                  IconText(Icons.place_outlined, request.cityName),
                  IconText(Icons.schedule, formatDateTime(request.desiredStartUtc)),
                  IconText(Icons.euro, "do ${formatMoney(request.maximumBudget)}"),
                  if (request.targetTradespersonProfileId != null)
                    const IconText(Icons.person_pin_outlined, "Upućen vama"),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget buildResults() {
    if (isLoading && requests.isEmpty) return const LoadingView();
    if (error != null) return ErrorView(message: error!, onRetry: loadRequests);
    if (requests.isEmpty) {
      return const EmptyView(message: "Trenutno nema novih zahtjeva za vas.", icon: Icons.work_outline);
    }
    return Column(
      children: [
        for (var request in requests)
          Padding(padding: const EdgeInsets.only(bottom: 12), child: buildRequestCard(request)),
        Card(
          child: Pager(
            page: page,
            pageSize: pageSize,
            totalCount: totalCount,
            onPageChanged: (newPage) {
              page = newPage;
              loadRequests();
            },
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Radni prostor",
      child: RefreshIndicator(
        onRefresh: loadRequests,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Card(
              child: ListTile(
                minTileHeight: 56,
                leading: const Icon(Icons.engineering_outlined, color: AppTheme.ink),
                title: const Text("Aktivne usluge"),
                subtitle: const Text("Dogovoreni poslovi koji su u toku"),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(builder: (context) => const AppointmentsScreen(active: true)),
                  );
                },
              ),
            ),
            const SectionTitle("Novi zahtjevi"),
            buildResults(),
          ],
        ),
      ),
    );
  }
}
