import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/category/category.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_mobile/models/tradesperson_profile/tradesperson_profile_search_request.dart';
import 'package:majstorio_mobile/providers/category/category_provider.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/providers/service_request/service_request_provider.dart';
import 'package:majstorio_mobile/providers/tradesperson_profile/tradesperson_profile_provider.dart';
import 'package:majstorio_mobile/screens/news_post/news_post_screen.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_details_screen.dart';
import 'package:majstorio_mobile/screens/tradesperson_profile/tradesperson_profiles_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:majstorio_mobile/widgets/tradesperson_card.dart';
import 'package:provider/provider.dart';

// "Početna" for a customer: search, the service categories, the recommended tradespeople and the
// request that is currently running. Each part loads on its own, so one failing part does not hide the others.
class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late CategoryProvider _categoryProvider;
  late ServiceRequestProvider _serviceRequestProvider;
  late OfferProvider _offerProvider;
  late TradespersonProfileProvider _profileProvider;

  static const recommendedCount = 3;
  List<TradespersonProfile>? recommended;
  String? recommendedError;

  List<Category>? categories;
  String? categoriesError;

  ServiceRequest? activeRequest;
  var isRequestLoading = true;
  String? requestError;

  @override
  void initState() {
    _categoryProvider = context.read<CategoryProvider>();
    _serviceRequestProvider = context.read<ServiceRequestProvider>();
    _offerProvider = context.read<OfferProvider>();
    _profileProvider = context.read<TradespersonProfileProvider>();
    // A search or a new request is new history, so the recommendations are loaded again.
    _profileProvider.searches.addListener(loadRecommended);
    _serviceRequestProvider.addListener(loadRecommended);
    // A new, changed or cancelled request (or an accepted offer) changes the active request card.
    _serviceRequestProvider.addListener(loadActiveRequest);
    _offerProvider.addListener(loadActiveRequest);
    loadCategories();
    loadActiveRequest();
    loadRecommended();
    super.initState();
  }

  @override
  void dispose() {
    _serviceRequestProvider.removeListener(loadActiveRequest);
    _offerProvider.removeListener(loadActiveRequest);
    _profileProvider.searches.removeListener(loadRecommended);
    _serviceRequestProvider.removeListener(loadRecommended);
    super.dispose();
  }

  Future<void> loadCategories() async {
    setState(() {
      categoriesError = null;
    });
    try {
      var result = await _categoryProvider.getActive();
      if (!mounted) return;
      setState(() {
        categories = result;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        categoriesError = exceptionText(e);
      });
    }
  }

  Future<void> loadActiveRequest() async {
    setState(() {
      isRequestLoading = true;
      requestError = null;
    });
    try {
      var result = await _serviceRequestProvider.getLatestActive();
      if (!mounted) return;
      setState(() {
        activeRequest = result;
        isRequestLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        requestError = exceptionText(e);
        isRequestLoading = false;
      });
    }
  }

  // The first results of a search without filters are the recommendations; the API does not log it.
  Future<void> loadRecommended() async {
    setState(() {
      recommendedError = null;
    });
    try {
      // One extra row, because a user's own profile can be in the list.
      var result = await _profileProvider.get(
        TradespersonProfileSearchRequest(pageSize: recommendedCount + 1),
      );
      if (!mounted) return;
      setState(() {
        recommended = result.items
            .where((profile) => profile.isApproved && profile.isActive && profile.userId != AuthSession.userId)
            .take(recommendedCount)
            .toList();
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        recommendedError = exceptionText(e);
      });
    }
  }

  void openSearch({int? categoryId}) {
    Navigator.push(
      context,
      MaterialPageRoute(builder: (context) => TradespersonProfilesScreen(initialCategoryId: categoryId)),
    );
  }

  Widget buildCategories() {
    if (categoriesError != null) {
      return ErrorView(message: categoriesError!, onRetry: loadCategories);
    }
    if (categories == null) return const LoadingView();
    if (categories!.isEmpty) return const EmptyView(message: "Nema aktivnih kategorija.");
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        for (var category in categories!)
          ActionChip(
            avatar: const Icon(Icons.handyman_outlined, size: 18, color: AppTheme.ink),
            label: Text(category.name),
            onPressed: () => openSearch(categoryId: category.id),
          ),
      ],
    );
  }

  Widget buildRecommended() {
    if (recommendedError != null) {
      return ErrorView(message: recommendedError!, onRetry: loadRecommended);
    }
    if (recommended == null) return const LoadingView();
    if (recommended!.isEmpty) return const EmptyView(message: "Trenutno nema preporučenih majstora.");
    return Column(
      children: [
        for (var profile in recommended!)
          Padding(padding: const EdgeInsets.only(bottom: 12), child: TradespersonCard(profile: profile)),
      ],
    );
  }

  Widget buildActiveRequest() {
    if (isRequestLoading && activeRequest == null) return const LoadingView();
    if (requestError != null) {
      return ErrorView(message: requestError!, onRetry: loadActiveRequest);
    }
    var request = activeRequest;
    if (request == null) {
      return const Card(
        child: Padding(
          padding: EdgeInsets.all(20),
          child: Text("Nemate aktivnih zahtjeva.", style: TextStyle(color: AppTheme.textMuted)),
        ),
      );
    }
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
                  StatusChip(request.statusName, highlighted: true),
                ],
              ),
              const SizedBox(height: 8),
              Text(request.description, maxLines: 2, overflow: TextOverflow.ellipsis),
              const SizedBox(height: 8),
              Row(
                children: [
                  const Icon(Icons.place_outlined, size: 16, color: AppTheme.textMuted),
                  const SizedBox(width: 4),
                  Text(request.cityName, style: const TextStyle(color: AppTheme.textMuted)),
                  const SizedBox(width: 16),
                  const Icon(Icons.schedule, size: 16, color: AppTheme.textMuted),
                  const SizedBox(width: 4),
                  Expanded(
                    child: Text(
                      formatDateTime(request.desiredStartUtc),
                      style: const TextStyle(color: AppTheme.textMuted),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Početna",
      actions: [
        IconButton(
          tooltip: "Novosti",
          icon: const Icon(Icons.article_outlined),
          onPressed: () {
            Navigator.push(context, MaterialPageRoute(builder: (context) => const NewsPostScreen()));
          },
        ),
      ],
      child: RefreshIndicator(
        onRefresh: () => Future.wait([loadCategories(), loadRecommended(), loadActiveRequest()]),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            OutlinedButton.icon(
              style: OutlinedButton.styleFrom(
                minimumSize: const Size.fromHeight(56),
                alignment: Alignment.centerLeft,
                backgroundColor: Colors.white,
                side: const BorderSide(color: AppTheme.border),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
              onPressed: () => openSearch(),
              icon: const Icon(Icons.search, color: AppTheme.textMuted),
              label: const Text("Pretraži majstore", style: TextStyle(color: AppTheme.textMuted)),
            ),
            const SectionTitle("Kategorije"),
            buildCategories(),
            const SectionTitle("Aktivni zahtjev"),
            buildActiveRequest(),
            const SectionTitle("Preporučeno za vas"),
            buildRecommended(),
          ],
        ),
      ),
    );
  }
}
