import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/offer/offer.dart';
import 'package:majstorio_mobile/models/offer/offer_upsert_request.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/utils/validators.dart' as validate;
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:provider/provider.dart';

// The tradesperson's offer on a request: create a new one, or edit one that is still pending.
// Pops true when the offer was saved.
class OfferDetailsScreen extends StatefulWidget {
  const OfferDetailsScreen({super.key, required this.request, this.toUpdate});

  final ServiceRequest request;
  final Offer? toUpdate;

  @override
  State<OfferDetailsScreen> createState() => _OfferDetailsScreenState();
}

class _OfferDetailsScreenState extends State<OfferDetailsScreen> {
  final key = GlobalKey<FormState>();
  final _price = TextEditingController();
  final _duration = TextEditingController();
  late OfferProvider _offerProvider;

  DateTime? proposedStart;
  var isSaving = false;
  var serverErrors = <String, String>{};

  bool get isEdit => widget.toUpdate != null;

  @override
  void initState() {
    _offerProvider = context.read<OfferProvider>();
    var offer = widget.toUpdate;
    if (offer != null) {
      _price.text = offer.price.toStringAsFixed(2);
      _duration.text = offer.durationMinutes.toString();
      proposedStart = offer.proposedStartUtc.toLocal();
    } else if (widget.request.desiredStartUtc.isAfter(DateTime.now().toUtc())) {
      // The customer's wish is a good first suggestion.
      proposedStart = widget.request.desiredStartUtc.toLocal();
    }
    super.initState();
  }

  @override
  void dispose() {
    _price.dispose();
    _duration.dispose();
    super.dispose();
  }

  void pickProposedStart() async {
    var now = DateTime.now();
    var initial = proposedStart != null && proposedStart!.isAfter(now) ? proposedStart! : now.add(const Duration(days: 1));
    var date = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: now.add(const Duration(days: 365)),
      helpText: "Datum početka",
    );
    if (date == null || !mounted) return;
    var time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(initial),
      helpText: "Vrijeme početka",
    );
    if (time == null) return;
    setState(() {
      proposedStart = DateTime(date.year, date.month, date.day, time.hour, time.minute);
    });
  }

  String? validateDuration(String? value) {
    var text = (value ?? "").trim();
    if (text.isEmpty) return "Trajanje je obavezno";
    var minutes = int.tryParse(text);
    if (minutes == null) return "Unesite cijeli broj minuta";
    if (minutes <= 0) return "Trajanje mora biti veće od 0";
    return serverErrors["durationMinutes"];
  }

  void save() async {
    setState(() {
      serverErrors = {};
    });
    if (key.currentState?.validate() != true) return;

    var price = validate.parseMoney(_price.text);
    var confirmed = await showConfirmDialog(
      context,
      "${isEdit ? "Spremiti izmjene ponude" : "Poslati ponudu"}: ${formatMoney(price)}, "
      "početak ${formatDateTime(proposedStart!)}, trajanje ${formatDuration(int.parse(_duration.text.trim()))}?",
    );
    if (!confirmed || !mounted) return;

    setState(() {
      isSaving = true;
    });
    var request = OfferUpsertRequest(
      serviceRequestId: isEdit ? null : widget.request.id,
      price: price,
      proposedStart: proposedStart!,
      durationMinutes: int.parse(_duration.text.trim()),
    );
    try {
      if (isEdit) {
        await _offerProvider.update(widget.toUpdate!.id, request);
      } else {
        await _offerProvider.insert(request);
      }
      if (!mounted) return;
      showMessage(context, isEdit ? "Ponuda je izmijenjena." : "Ponuda je poslana.");
      Navigator.pop(context, true);
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        isSaving = false;
        serverErrors = e.fieldErrors;
      });
      showMessage(context, e.message);
      if (e.statusCode == 409 || e.statusCode == 404 || e.statusCode == 403) {
        // The request or offer changed meanwhile, or this tradesperson may not quote it: back to the request.
        Navigator.pop(context, true);
        return;
      }
      key.currentState?.validate();
    }
  }

  @override
  Widget build(BuildContext context) {
    var request = widget.request;
    return MasterScreen(
      title: isEdit ? "Izmjena ponude" : "Nova ponuda",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Form(
              key: key,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  FormHeader(
                    icon: Icons.request_quote_outlined,
                    title: isEdit ? "Izmjena ponude" : "Nova ponuda",
                    subtitle: "${request.serviceCategoryName} · ${request.cityName}",
                  ),
                  const SizedBox(height: 16),
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(color: AppTheme.goldSoft, borderRadius: BorderRadius.circular(10)),
                    child: Text(
                      "Budžet korisnika: do ${formatMoney(request.maximumBudget)}\n"
                      "Željeni početak: ${formatDateTime(request.desiredStartUtc)}",
                      style: const TextStyle(color: AppTheme.ink),
                    ),
                  ),
                  const SizedBox(height: 24),
                  TextFormField(
                    controller: _price,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: const InputDecoration(labelText: "Cijena (EUR)", prefixIcon: Icon(Icons.euro)),
                    validator: (value) =>
                        validate.money(value, max: request.maximumBudget) ?? serverErrors["price"],
                  ),
                  const SizedBox(height: 16),
                  FormField<DateTime>(
                    validator: (value) {
                      if (proposedStart == null) return "Odaberite početak";
                      if (!proposedStart!.isAfter(DateTime.now())) return "Početak mora biti u budućnosti";
                      return serverErrors["proposedStartUtc"];
                    },
                    builder: (field) {
                      return InkWell(
                        onTap: pickProposedStart,
                        borderRadius: BorderRadius.circular(10),
                        child: InputDecorator(
                          decoration: InputDecoration(
                            labelText: "Predloženi početak",
                            prefixIcon: const Icon(Icons.event_outlined),
                            suffixIcon: const Icon(Icons.edit_calendar_outlined),
                            errorText: field.errorText,
                          ),
                          child: Text(
                            proposedStart == null ? "Odaberite datum i vrijeme" : formatDateTime(proposedStart!),
                          ),
                        ),
                      );
                    },
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _duration,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: "Trajanje (minuta)",
                      prefixIcon: Icon(Icons.timer_outlined),
                    ),
                    validator: validateDuration,
                  ),
                  const SizedBox(height: 24),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: isSaving ? null : save,
                      icon: Icon(isEdit ? Icons.save_outlined : Icons.send_outlined),
                      label: Text(isSaving ? "Spremanje..." : (isEdit ? "Spremi izmjene" : "Pošalji ponudu")),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
