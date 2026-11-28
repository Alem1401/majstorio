import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/main_navigation.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/category/category.dart';
import 'package:majstorio_mobile/models/city/city.dart';
import 'package:majstorio_mobile/models/service_request/service_request_upsert_request.dart';
import 'package:majstorio_mobile/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_mobile/providers/category/category_provider.dart';
import 'package:majstorio_mobile/providers/city/city_provider.dart';
import 'package:majstorio_mobile/providers/service_request/service_request_provider.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/utils/validators.dart' as validate;
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:provider/provider.dart';

// Create or edit a request.
// - From "Zahtjevi": a general request that every matching tradesperson can see.
// - From a tradesperson's profile ("target"): a request for that person; city and category are limited to theirs.
// Pops true when the request was saved.
class ServiceRequestFormScreen extends StatefulWidget {
  const ServiceRequestFormScreen({super.key, this.target});

  final TradespersonProfileDetail? target;

  @override
  State<ServiceRequestFormScreen> createState() => _ServiceRequestFormScreenState();
}

class _ServiceRequestFormScreenState extends State<ServiceRequestFormScreen> {
  final key = GlobalKey<FormState>();
  final _description = TextEditingController();
  final _address = TextEditingController();
  final _budget = TextEditingController();
  late ServiceRequestProvider _serviceRequestProvider;
  late CityProvider _cityProvider;
  late CategoryProvider _categoryProvider;

  // The choices of the two pickers, from the API.
  List<City>? cities;
  List<Category>? categories;
  String? loadError;

  int? cityId;
  int? categoryId;
  DateTime? desiredStart;

  var isSaving = false;
  // Errors the API returned for single fields, shown under those fields.
  var serverErrors = <String, String>{};

  @override
  void initState() {
    _serviceRequestProvider = context.read<ServiceRequestProvider>();
    _cityProvider = context.read<CityProvider>();
    _categoryProvider = context.read<CategoryProvider>();

    var target = widget.target;
    if (target != null) {
      cityId = target.profile.cityId;
      // With one category there is nothing to choose.
      if (target.serviceCategories.length == 1) categoryId = target.serviceCategories.keys.first;
    }

    loadChoices();
    super.initState();
  }

  @override
  void dispose() {
    _description.dispose();
    _address.dispose();
    _budget.dispose();
    super.dispose();
  }

  // Only active cities and categories can be chosen. For a chosen tradesperson, only theirs.
  Future<void> loadChoices() async {
    setState(() {
      loadError = null;
    });
    try {
      var results = await Future.wait([_cityProvider.getActive(), _categoryProvider.getActive()]);
      var allCities = results[0] as List<City>;
      var allCategories = results[1] as List<Category>;

      var target = widget.target;
      if (target != null) {
        allCities = allCities.where((city) => city.id == target.profile.cityId).toList();
        allCategories = allCategories.where((category) => target.serviceCategories.containsKey(category.id)).toList();
      }

      if (!mounted) return;
      setState(() {
        cities = allCities;
        categories = allCategories;
        // A choice that is no longer active is cleared, so the form asks for a new one.
        if (!allCities.any((city) => city.id == cityId)) cityId = null;
        if (!allCategories.any((category) => category.id == categoryId)) categoryId = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        loadError = exceptionText(e);
      });
    }
  }

  void pickDesiredStart() async {
    var now = DateTime.now();
    var initial = desiredStart != null && desiredStart!.isAfter(now) ? desiredStart! : now.add(const Duration(days: 1));
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
      desiredStart = DateTime(date.year, date.month, date.day, time.hour, time.minute);
    });
  }

  String? validateDesiredStart() {
    if (desiredStart == null) return "Odaberite željeni početak";
    if (!desiredStart!.isAfter(DateTime.now())) return "Početak mora biti u budućnosti";
    return serverErrors["desiredStartUtc"];
  }

  void save() async {
    setState(() {
      serverErrors = {};
    });
    if (key.currentState?.validate() != true) return;

    var confirmed = await showConfirmDialog(
      context,
      "Poslati zahtjev? Majstori će ga vidjeti i moći poslati ponude.",
    );
    if (!confirmed || !mounted) return;

    setState(() {
      isSaving = true;
    });
    var request = ServiceRequestUpsertRequest(
      serviceCategoryId: categoryId!,
      cityId: cityId!,
      description: _description.text.trim(),
      address: _address.text.trim(),
      desiredStart: desiredStart!,
      maximumBudget: validate.parseMoney(_budget.text),
      targetTradespersonProfileId: widget.target?.profile.id,
    );

    try {
      await _serviceRequestProvider.insert(request);
      if (!mounted) return;
      showMessage(context, "Zahtjev je poslan.");
      if (widget.target != null) {
        // Sent from a profile: open "Zahtjevi", where the new request is at the top.
        Navigator.pushAndRemoveUntil(
          context,
          MaterialPageRoute(builder: (context) => const MainNavigation(initialIndex: 2)),
          (route) => false,
        );
      } else {
        Navigator.pop(context, true);
      }
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        isSaving = false;
        serverErrors = e.fieldErrors;
      });
      showMessage(context, e.message);
      if (e.statusCode == 409 || e.statusCode == 404 || e.statusCode == 403) {
        // The request changed in the meantime (for example an offer arrived): back to the detail, which reloads.
        Navigator.pop(context, true);
        return;
      }
      key.currentState?.validate();
    }
  }

  Widget buildTargetCard() {
    var name = widget.target?.profile.fullName;
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppTheme.goldSoft,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        children: [
          const Icon(Icons.person_pin_outlined, color: AppTheme.ink),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              name != null ? "Zahtjev za majstora: $name" : "Zahtjev je poslan odabranom majstoru.",
              style: const TextStyle(color: AppTheme.ink),
            ),
          ),
        ],
      ),
    );
  }

  Widget buildForm() {
    return Form(
      key: key,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          FormHeader(
            icon: Icons.assignment_outlined,
            title: "Novi zahtjev",
            subtitle: "Opišite posao, kada i za koliko ga želite.",
          ),
          const SizedBox(height: 24),
          const Divider(),
          const SizedBox(height: 24),
          if (widget.target != null) buildTargetCard(),
          DropdownButtonFormField<int>(
            initialValue: categoryId,
            isExpanded: true,
            decoration: const InputDecoration(labelText: "Kategorija", prefixIcon: Icon(Icons.handyman_outlined)),
            items: [
              for (var category in categories!) DropdownMenuItem(value: category.id, child: Text(category.name)),
            ],
            onChanged: (value) => setState(() => categoryId = value),
            validator: (value) => value == null ? "Odaberite kategoriju" : serverErrors["serviceCategoryId"],
          ),
          const SizedBox(height: 16),
          DropdownButtonFormField<int>(
            initialValue: cityId,
            isExpanded: true,
            decoration: const InputDecoration(labelText: "Grad", prefixIcon: Icon(Icons.place_outlined)),
            items: [for (var city in cities!) DropdownMenuItem(value: city.id, child: Text(city.name))],
            onChanged: widget.target != null ? null : (value) => setState(() => cityId = value),
            validator: (value) => value == null ? "Odaberite grad" : serverErrors["cityId"],
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _description,
            minLines: 3,
            maxLines: 6,
            maxLength: 2000,
            decoration: const InputDecoration(
              labelText: "Opis problema",
              prefixIcon: Icon(Icons.description_outlined),
              alignLabelWithHint: true,
            ),
            validator: (value) =>
                validate.requiredText(value, "Opis je obavezan") ?? serverErrors["description"],
          ),
          const SizedBox(height: 8),
          TextFormField(
            controller: _address,
            maxLength: 300,
            decoration: const InputDecoration(labelText: "Adresa (nije obavezno)", prefixIcon: Icon(Icons.home_outlined)),
            validator: (value) => serverErrors["address"],
          ),
          const SizedBox(height: 8),
          FormField<DateTime>(
            validator: (value) => validateDesiredStart(),
            builder: (field) {
              return InkWell(
                onTap: () async {
                  pickDesiredStart();
                },
                borderRadius: BorderRadius.circular(10),
                child: InputDecorator(
                  decoration: InputDecoration(
                    labelText: "Željeni početak",
                    prefixIcon: const Icon(Icons.event_outlined),
                    suffixIcon: const Icon(Icons.edit_calendar_outlined),
                    errorText: field.errorText,
                  ),
                  child: Text(desiredStart == null ? "Odaberite datum i vrijeme" : formatDateTime(desiredStart!)),
                ),
              );
            },
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _budget,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(
              labelText: "Najveći budžet (EUR)",
              prefixIcon: Icon(Icons.euro),
            ),
            validator: (value) => validate.money(value) ?? serverErrors["maximumBudget"],
          ),
          const SizedBox(height: 24),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: isSaving ? null : save,
              icon: const Icon(Icons.send_outlined),
              label: Text(isSaving ? "Spremanje..." : "Pošalji zahtjev"),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    Widget body;
    if (loadError != null) {
      body = ErrorView(message: loadError!, onRetry: loadChoices);
    } else if (cities == null || categories == null) {
      body = const LoadingView();
    } else if (cities!.isEmpty || categories!.isEmpty) {
      // Without a city and a category a request cannot be created, so the form is not shown.
      body = EmptyView(
        message: widget.target != null
            ? "Ovaj majstor trenutno nema aktivnu kategoriju ili grad. Zahtjev se ne može poslati."
            : "Nema aktivnih gradova ili kategorija. Zahtjev se trenutno ne može kreirati.",
        icon: Icons.block,
      );
    } else {
      body = SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Card(child: Padding(padding: const EdgeInsets.all(20), child: buildForm())),
      );
    }
    return MasterScreen(title: "Novi zahtjev", child: body);
  }
}
