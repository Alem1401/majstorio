import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/offer/offer.dart';
import 'package:majstorio_mobile/models/offer/offer_search_request.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_details_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:majstorio_mobile/widgets/pager.dart';
import 'package:provider/provider.dart';

// "Ponude" for a tradesperson: the offers they sent, newest first, filtered by status.
// Opening an offer shows its request, where a pending offer can be edited or withdrawn.
class OffersScreen extends StatefulWidget {
  const OffersScreen({super.key});

  @override
  State<OffersScreen> createState() => _OffersScreenState();
}

class _OffersScreenState extends State<OffersScreen> {
  late OfferProvider _offerProvider;

  var offers = <Offer>[];
  var isLoading = true;
  String? error;
  int? status;

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;
  var _loadNumber = 0;

  @override
  void initState() {
    _offerProvider = context.read<OfferProvider>();
    _offerProvider.addListener(loadOffers);
    loadOffers();
    super.initState();
  }

  @override
  void dispose() {
    _offerProvider.removeListener(loadOffers);
    super.dispose();
  }

  Future<void> loadOffers() async {
    var loadNumber = ++_loadNumber;
    setState(() {
      isLoading = true;
      error = null;
    });
    try {
      var response = await _offerProvider.get(OfferSearchRequest(page: page, pageSize: pageSize, status: status));
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        offers = response.items;
        totalCount = response.totalCount;
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

  Widget buildOfferCard(Offer offer) {
    return Card(
      child: InkWell(
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (context) => ServiceRequestDetailsScreen(requestId: offer.serviceRequestId)),
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
                      offer.serviceCategoryName,
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: AppTheme.ink),
                    ),
                  ),
                  StatusChip(offer.statusName, highlighted: offer.isPending || offer.status == Offer.accepted),
                ],
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 16,
                runSpacing: 4,
                children: [
                  IconText(Icons.person_outline, offer.customerName),
                  IconText(Icons.euro, formatMoney(offer.price)),
                  IconText(Icons.schedule, formatDateTime(offer.proposedStartUtc)),
                  IconText(Icons.timer_outlined, formatDuration(offer.durationMinutes)),
                ],
              ),
              if (offer.decisionReason != null && offer.decisionReason!.isNotEmpty) ...[
                const SizedBox(height: 8),
                Text("Razlog: ${offer.decisionReason}", style: const TextStyle(color: AppTheme.textMuted)),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget buildResults() {
    if (isLoading && offers.isEmpty) return const LoadingView();
    if (error != null) return ErrorView(message: error!, onRetry: loadOffers);
    if (offers.isEmpty) {
      return EmptyView(
        message: status == null ? "Još niste poslali ponudu." : "Nema ponuda sa ovim statusom.",
        icon: Icons.request_quote_outlined,
      );
    }
    return Column(
      children: [
        for (var offer in offers) Padding(padding: const EdgeInsets.only(bottom: 12), child: buildOfferCard(offer)),
        Card(
          child: Pager(
            page: page,
            pageSize: pageSize,
            totalCount: totalCount,
            onPageChanged: (newPage) {
              page = newPage;
              loadOffers();
            },
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    var choices = <int?, String>{null: "Sve", ...Offer.statusNames};
    return MasterScreen(
      title: "Ponude",
      child: RefreshIndicator(
        onRefresh: loadOffers,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  for (var choice in choices.entries)
                    Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: ChoiceChip(
                        label: Text(choice.value),
                        selected: status == choice.key,
                        onSelected: (selected) {
                          if (!selected) return;
                          setState(() {
                            status = choice.key;
                          });
                          page = 1;
                          loadOffers();
                        },
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            buildResults(),
          ],
        ),
      ),
    );
  }
}
