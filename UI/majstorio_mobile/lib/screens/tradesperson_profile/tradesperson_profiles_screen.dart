import 'dart:async';

import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/category/category.dart';
import 'package:majstorio_mobile/models/city/city.dart';
import 'package:majstorio_mobile/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_mobile/models/tradesperson_profile/tradesperson_profile_search_request.dart';
import 'package:majstorio_mobile/providers/category/category_provider.dart';
import 'package:majstorio_mobile/providers/city/city_provider.dart';
import 'package:majstorio_mobile/providers/tradesperson_profile/tradesperson_profile_provider.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:majstorio_mobile/widgets/pager.dart';
import 'package:majstorio_mobile/widgets/tradesperson_card.dart';
import 'package:provider/provider.dart';

// "Pretraga": tradespeople by name, with city, category and minimum rating filters.
// The API sorts the results by its recommendation score and says why.
class TradespersonProfilesScreen extends StatefulWidget {
  const TradespersonProfilesScreen({super.key, this.initialCategoryId});

  // Set when the search is opened from a category on the home screen.
  final int? initialCategoryId;

  @override
  State<TradespersonProfilesScreen> createState() => _TradespersonProfilesScreenState();
}

class _TradespersonProfilesScreenState extends State<TradespersonProfilesScreen> {
  late TradespersonProfileProvider _profileProvider;
  late CityProvider _cityProvider;
  late CategoryProvider _categoryProvider;
  final _searchController = TextEditingController();
  Timer? _debouncer;

  var profiles = <TradespersonProfile>[];
  var isLoading = true;
  String? error;

  // The choices of the filter sheet.
  var cities = <City>[];
  var categories = <Category>[];

  // The filters that are applied.
  int? cityId;
  int? categoryId;
  int? minimumRating;
  // Local midnight of the chosen day; only tradespeople free that whole day are shown.
  DateTime? availableDay;

  // Paging: which page is shown and how many rows the server has in total.
  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  // Each load gets a number; an answer that arrives after a newer load started is ignored.
  var _loadNumber = 0;

  @override
  void initState() {
    _profileProvider = context.read<TradespersonProfileProvider>();
    _cityProvider = context.read<CityProvider>();
    _categoryProvider = context.read<CategoryProvider>();
    categoryId = widget.initialCategoryId;
    loadChoices();
    loadProfiles();
    super.initState();
  }

  @override
  void dispose() {
    _debouncer?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  // Cities and categories for the filter sheet. If they fail, the sheet says so; the search still works.
  void loadChoices() async {
    try {
      var results = await Future.wait([_cityProvider.getActive(), _categoryProvider.getActive()]);
      if (!mounted) return;
      setState(() {
        cities = results[0] as List<City>;
        categories = results[1] as List<Category>;
      });
    } catch (e) {
      // Shown in the filter sheet as "no choices".
    }
  }

  // Sends the page, the search text and the filters to the API. The search field, the filters and the pager call this.
  Future<void> loadProfiles() async {
    var loadNumber = ++_loadNumber;
    setState(() {
      isLoading = true;
      error = null;
    });
    try {
      var response = await _profileProvider.get(
        TradespersonProfileSearchRequest(
          page: page,
          pageSize: pageSize,
          name: _searchController.text.trim(),
          cityId: cityId,
          serviceCategoryId: categoryId,
          minimumRating: minimumRating,
          availableFromUtc: availableDay?.toUtc(),
          availableToUtc: availableDay == null
              ? null
              : DateTime(availableDay!.year, availableDay!.month, availableDay!.day + 1).toUtc(),
        ),
      );
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        profiles = response.items;
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

  // Waits until the user stops typing, then searches from the first page.
  void searchProfiles(String searchTerm) {
    _debouncer?.cancel();
    _debouncer = Timer(const Duration(milliseconds: 400), () {
      page = 1;
      loadProfiles();
    });
  }

  String? cityName(int? id) => cities.where((city) => city.id == id).map((city) => city.name).firstOrNull;
  String? categoryName(int? id) =>
      categories.where((category) => category.id == id).map((category) => category.name).firstOrNull;

  int get activeFilterCount => [cityId, categoryId, minimumRating].where((value) => value != null).length;

  // The filter sheet. The choices are only applied when "Primijeni" is tapped.
  void openFilters() async {
    int? newCityId = cityId;
    int? newCategoryId = categoryId;
    int? newMinimumRating = minimumRating;

    var apply = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setSheetState) {
            return SafeArea(
              child: Padding(
                padding: EdgeInsets.fromLTRB(16, 0, 16, 16 + MediaQuery.of(context).viewInsets.bottom),
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        "Filteri",
                        style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700, color: AppTheme.ink),
                      ),
                      if (cities.isEmpty || categories.isEmpty) ...[
                        const SizedBox(height: 8),
                        const Text(
                          "Gradovi ili kategorije nisu učitani.",
                          style: TextStyle(color: AppTheme.danger),
                        ),
                      ],
                      const SizedBox(height: 16),
                      DropdownButtonFormField<int?>(
                        initialValue: newCityId,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: "Grad", prefixIcon: Icon(Icons.place_outlined)),
                        items: [
                          const DropdownMenuItem<int?>(value: null, child: Text("Svi gradovi")),
                          for (var city in cities) DropdownMenuItem<int?>(value: city.id, child: Text(city.name)),
                        ],
                        onChanged: (value) => setSheetState(() => newCityId = value),
                      ),
                      const SizedBox(height: 16),
                      DropdownButtonFormField<int?>(
                        initialValue: newCategoryId,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: "Kategorija", prefixIcon: Icon(Icons.handyman_outlined)),
                        items: [
                          const DropdownMenuItem<int?>(value: null, child: Text("Sve kategorije")),
                          for (var category in categories)
                            DropdownMenuItem<int?>(value: category.id, child: Text(category.name)),
                        ],
                        onChanged: (value) => setSheetState(() => newCategoryId = value),
                      ),
                      const SizedBox(height: 16),
                      DropdownButtonFormField<int?>(
                        initialValue: newMinimumRating,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: "Najmanja ocjena", prefixIcon: Icon(Icons.star_outline)),
                        items: [
                          const DropdownMenuItem<int?>(value: null, child: Text("Bilo koja")),
                          for (var rating = 1; rating <= 5; rating++)
                            DropdownMenuItem<int?>(value: rating, child: Text("$rating ★ i više")),
                        ],
                        onChanged: (value) => setSheetState(() => newMinimumRating = value),
                      ),
                      const SizedBox(height: 24),
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton(
                              style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(52)),
                              onPressed: () {
                                newCityId = null;
                                newCategoryId = null;
                                newMinimumRating = null;
                                Navigator.pop(context, true);
                              },
                              child: const Text("Poništi"),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: ElevatedButton(
                              onPressed: () => Navigator.pop(context, true),
                              child: const Text("Primijeni"),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            );
          },
        );
      },
    );

    if (apply == true) {
      setState(() {
        cityId = newCityId;
        categoryId = newCategoryId;
        minimumRating = newMinimumRating;
      });
      page = 1;
      loadProfiles();
    }
  }

  // Picks the day the tradesperson must be free; showDatePicker returns local midnight.
  void pickAvailableDay() async {
    var now = DateTime.now();
    var day = await showDatePicker(
      context: context,
      initialDate: availableDay ?? now,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: now.add(const Duration(days: 365)),
      helpText: "Slobodan na dan",
    );
    if (day == null || !mounted) return;
    setState(() => availableDay = day);
    page = 1;
    loadProfiles();
  }

  // The applied filters as chips; tapping the x removes that filter.
  Widget buildActiveFilters() {
    var chips = <Widget>[
      if (cityId != null)
        InputChip(
          label: Text(cityName(cityId) ?? "Grad"),
          onDeleted: () {
            setState(() => cityId = null);
            page = 1;
            loadProfiles();
          },
        ),
      if (categoryId != null)
        InputChip(
          label: Text(categoryName(categoryId) ?? "Kategorija"),
          onDeleted: () {
            setState(() => categoryId = null);
            page = 1;
            loadProfiles();
          },
        ),
      if (minimumRating != null)
        InputChip(
          label: Text("$minimumRating ★ i više"),
          onDeleted: () {
            setState(() => minimumRating = null);
            page = 1;
            loadProfiles();
          },
        ),
    ];
    if (chips.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.only(top: 12),
      child: Wrap(spacing: 8, runSpacing: 8, children: chips),
    );
  }

  Widget buildResults() {
    if (isLoading && profiles.isEmpty) return const LoadingView();
    if (error != null) return ErrorView(message: error!, onRetry: loadProfiles);
    if (profiles.isEmpty) {
      return EmptyView(
        message: activeFilterCount > 0 || _searchController.text.isNotEmpty
            ? "Nema majstora za odabrane filtere."
            : "Nema dostupnih majstora.",
        icon: Icons.search_off,
      );
    }
    return Column(
      children: [
        for (var profile in profiles)
          Padding(padding: const EdgeInsets.only(bottom: 12), child: TradespersonCard(profile: profile)),
        Card(
          child: Pager(
            page: page,
            pageSize: pageSize,
            totalCount: totalCount,
            onPageChanged: (newPage) {
              page = newPage;
              loadProfiles();
            },
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Pretraga",
      child: RefreshIndicator(
        onRefresh: loadProfiles,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    controller: _searchController,
                    onChanged: searchProfiles,
                    textInputAction: TextInputAction.search,
                    decoration: const InputDecoration(
                      labelText: "Ime majstora",
                      prefixIcon: Icon(Icons.search),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Badge(
                  isLabelVisible: activeFilterCount > 0,
                  label: Text("$activeFilterCount"),
                  backgroundColor: AppTheme.gold,
                  textColor: AppTheme.inkDeep,
                  child: IconButton.filledTonal(
                    tooltip: "Filteri",
                    iconSize: 28,
                    onPressed: openFilters,
                    icon: const Icon(Icons.tune),
                  ),
                ),
              ],
            ),
            buildActiveFilters(),
            const SizedBox(height: 8),
            Align(
              alignment: Alignment.centerLeft,
              child: availableDay == null
                  ? ActionChip(
                      avatar: const Icon(Icons.event_available, size: 18),
                      label: const Text("Slobodan na dan…"),
                      onPressed: pickAvailableDay,
                    )
                  : InputChip(
                      avatar: const Icon(Icons.event_available, size: 18),
                      label: Text("Slobodan ${formatDate(availableDay!)}"),
                      onPressed: pickAvailableDay,
                      onDeleted: () {
                        setState(() => availableDay = null);
                        page = 1;
                        loadProfiles();
                      },
                    ),
            ),
            const SizedBox(height: 12),
            const Text(
              "Poredano po preporuci za vas",
              style: TextStyle(fontSize: 12, color: AppTheme.textMuted),
            ),
            const SizedBox(height: 8),
            buildResults(),
          ],
        ),
      ),
    );
  }
}
