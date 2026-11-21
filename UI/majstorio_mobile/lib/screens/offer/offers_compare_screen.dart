import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/offer/offer.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:provider/provider.dart';

// The customer compares the pending offers of a request and accepts one.
// The choice stays on the phone until "Prihvati ponudu" is confirmed; then the API accepts it,
// closes the other offers and creates the appointment. Pops true after an accepted offer.
class OffersCompareScreen extends StatefulWidget {
  const OffersCompareScreen({super.key, required this.detail});

  final ServiceRequestDetail detail;

  @override
  State<OffersCompareScreen> createState() => _OffersCompareScreenState();
}

class _OffersCompareScreenState extends State<OffersCompareScreen> {
  late OfferProvider _offerProvider;

  int? selectedOfferId;
  var isSaving = false;

  List<Offer> get offers => widget.detail.pendingOffers;

  @override
  void initState() {
    _offerProvider = context.read<OfferProvider>();
    super.initState();
  }

  void accept() async {
    var offer = offers.firstWhere((offer) => offer.id == selectedOfferId);
    var confirmed = await showConfirmDialog(
      context,
      "Prihvatiti ponudu majstora ${offer.tradespersonName} za ${formatMoney(offer.price)}, "
      "početak ${formatDateTime(offer.proposedStartUtc)}? Ostale ponude će biti zatvorene.",
      title: "Prihvatanje ponude",
    );
    if (!confirmed || !mounted) return;

    setState(() {
      isSaving = true;
    });
    try {
      await _offerProvider.accept(offer.id);
      if (!mounted) return;
      showMessage(context, "Ponuda je prihvaćena. Termin je potvrđen.");
      Navigator.pop(context, true);
    } catch (e) {
      if (!mounted) return;
      showMessage(context, exceptionText(e));
      // For example: another offer was already accepted, or the tradesperson is busy at that time.
      // Back to the request, which loads its real state.
      Navigator.pop(context, true);
    }
  }

  Widget buildOffer(Offer offer) {
    var isSelected = offer.id == selectedOfferId;
    var cheapest = offers.map((offer) => offer.price).reduce((a, b) => a < b ? a : b);
    var earliest = offers.map((offer) => offer.proposedStartUtc).reduce((a, b) => a.isBefore(b) ? a : b);

    return Card(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: isSelected ? AppTheme.gold : AppTheme.border, width: isSelected ? 2 : 1),
      ),
      child: InkWell(
        onTap: isSaving ? null : () => setState(() => selectedOfferId = offer.id),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                isSelected ? Icons.radio_button_checked : Icons.radio_button_unchecked,
                color: isSelected ? AppTheme.gold : AppTheme.textMuted,
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      offer.tradespersonName,
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: AppTheme.ink),
                    ),
                    const SizedBox(height: 8),
                    InfoRow(icon: Icons.euro, label: "Cijena", value: formatMoney(offer.price)),
                    InfoRow(
                      icon: Icons.schedule,
                      label: "Predloženi početak",
                      value: formatDateTime(offer.proposedStartUtc),
                    ),
                    InfoRow(icon: Icons.timer_outlined, label: "Trajanje", value: formatDuration(offer.durationMinutes)),
                    Wrap(
                      spacing: 8,
                      runSpacing: 4,
                      children: [
                        if (offers.length > 1 && offer.price == cheapest) const StatusChip("Najniža cijena"),
                        if (offers.length > 1 && offer.proposedStartUtc == earliest) const StatusChip("Najraniji početak"),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    var request = widget.detail.request;
    Widget body;
    if (offers.isEmpty) {
      body = const EmptyView(message: "Nema ponuda na čekanju.", icon: Icons.request_quote_outlined);
    } else {
      body = ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            "${request.serviceCategoryName} · budžet do ${formatMoney(request.maximumBudget)}",
            style: const TextStyle(color: AppTheme.textMuted),
          ),
          const SizedBox(height: 12),
          for (var offer in offers) Padding(padding: const EdgeInsets.only(bottom: 12), child: buildOffer(offer)),
          const SizedBox(height: 8),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: selectedOfferId == null || isSaving ? null : accept,
              icon: const Icon(Icons.check_circle_outline),
              label: Text(isSaving ? "Prihvatanje..." : "Prihvati ponudu"),
            ),
          ),
          if (selectedOfferId == null) ...[
            const SizedBox(height: 8),
            const Text(
              "Odaberite jednu ponudu.",
              textAlign: TextAlign.center,
              style: TextStyle(color: AppTheme.textMuted),
            ),
          ],
        ],
      );
    }
    return MasterScreen(title: "Usporedba ponuda", child: body);
  }
}
