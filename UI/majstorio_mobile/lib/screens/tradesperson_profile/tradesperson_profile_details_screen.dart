import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_mobile/providers/tradesperson_profile/tradesperson_profile_provider.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_form_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:provider/provider.dart';

// "Profil majstora": services with prices, latest reviews and the "Pošalji zahtjev" button.
class TradespersonProfileDetailsScreen extends StatefulWidget {
  const TradespersonProfileDetailsScreen({super.key, required this.profileId});

  final int profileId;

  @override
  State<TradespersonProfileDetailsScreen> createState() => _TradespersonProfileDetailsScreenState();
}

class _TradespersonProfileDetailsScreenState extends State<TradespersonProfileDetailsScreen> {
  late TradespersonProfileProvider _profileProvider;

  TradespersonProfileDetail? detail;
  String? error;

  @override
  void initState() {
    _profileProvider = context.read<TradespersonProfileProvider>();
    loadProfile();
    super.initState();
  }

  Future<void> loadProfile() async {
    setState(() {
      error = null;
    });
    try {
      var result = await _profileProvider.getById(widget.profileId);
      if (!mounted) return;
      setState(() {
        detail = result;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        // 404: the profile does not exist or is not visible to this user. Nothing about it is shown.
        error = e is ApiException && e.statusCode == 404 ? "Ovaj profil nije dostupan." : exceptionText(e);
      });
    }
  }

  // Why the button is disabled, or null when the request can be sent.
  String? requestBlockedReason(TradespersonProfileDetail detail) {
    var profile = detail.profile;
    if (!AuthSession.isCustomer) return "Zahtjev mogu poslati samo korisnici.";
    if (profile.userId == AuthSession.userId) return "Ovo je vaš profil.";
    if (!profile.isApproved || !profile.isActive) return "Ovaj majstor trenutno ne prima zahtjeve.";
    if (detail.serviceCategories.isEmpty) return "Ovaj majstor nema aktivnih usluga.";
    return null;
  }

  Widget buildMetric(IconData icon, String value, String label) {
    return Expanded(
      child: Card(
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 8),
          child: Column(
            children: [
              Icon(icon, color: AppTheme.gold),
              const SizedBox(height: 6),
              Text(value, style: const TextStyle(fontWeight: FontWeight.w700, color: AppTheme.ink)),
              Text(label, style: const TextStyle(fontSize: 12, color: AppTheme.textMuted), textAlign: TextAlign.center),
            ],
          ),
        ),
      ),
    );
  }

  Widget buildContent(TradespersonProfileDetail detail) {
    var profile = detail.profile;
    var blockedReason = requestBlockedReason(detail);

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                NetworkPicture(url: profile.profileImageUrl, size: 80),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        profile.fullName,
                        style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700, color: AppTheme.ink),
                      ),
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          const Icon(Icons.place_outlined, size: 16, color: AppTheme.textMuted),
                          const SizedBox(width: 4),
                          Text(profile.cityName, style: const TextStyle(color: AppTheme.textMuted)),
                        ],
                      ),
                      if (profile.isApproved) ...[
                        const SizedBox(height: 8),
                        const StatusChip("Odobren profil", highlighted: true),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            buildMetric(
              Icons.star_outline,
              profile.averageRating == null ? "–" : profile.averageRating!.toStringAsFixed(1),
              "Ocjena",
            ),
            const SizedBox(width: 8),
            buildMetric(Icons.reviews_outlined, "${profile.reviewCount}", "Recenzija"),
            const SizedBox(width: 8),
            buildMetric(Icons.task_alt, "${profile.completedJobs}", "Završenih poslova"),
          ],
        ),
        if (profile.description.isNotEmpty) ...[
          const SectionTitle("O majstoru"),
          Text(profile.description),
        ],
        const SectionTitle("Usluge i cijene"),
        if (detail.services.isEmpty)
          const Text("Nema aktivnih usluga.", style: TextStyle(color: AppTheme.textMuted))
        else
          Card(
            child: Column(
              children: [
                for (var i = 0; i < detail.services.length; i++) ...[
                  if (i > 0) const Divider(),
                  ListTile(
                    title: Text(detail.services[i].name),
                    subtitle: Text(
                      "${detail.services[i].serviceCategoryName} · "
                      "oko ${formatDuration(detail.services[i].estimatedDurationMinutes)}",
                    ),
                    trailing: Text(
                      "${formatMoney(detail.services[i].minimumPrice)}\n– ${formatMoney(detail.services[i].maximumPrice)}",
                      textAlign: TextAlign.right,
                      style: const TextStyle(fontWeight: FontWeight.w600, color: AppTheme.ink),
                    ),
                  ),
                ],
              ],
            ),
          ),
        const SectionTitle("Posljednje recenzije"),
        if (detail.latestReviews.isEmpty)
          const Text("Još nema recenzija.", style: TextStyle(color: AppTheme.textMuted))
        else
          for (var review in detail.latestReviews)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Card(
                child: ListTile(
                  title: Text(review.customerName),
                  subtitle: review.comment == null || review.comment!.isEmpty ? null : Text(review.comment!),
                  trailing: Text(
                    "${review.rating} ★",
                    style: const TextStyle(fontWeight: FontWeight.w700, color: AppTheme.gold),
                  ),
                ),
              ),
            ),
        const SizedBox(height: 24),
        SizedBox(
          width: double.infinity,
          child: ElevatedButton.icon(
            onPressed: blockedReason != null
                ? null
                : () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(builder: (context) => ServiceRequestFormScreen(target: detail)),
                    );
                  },
            icon: const Icon(Icons.send_outlined),
            label: const Text("Pošalji zahtjev"),
          ),
        ),
        if (blockedReason != null) ...[
          const SizedBox(height: 8),
          Text(blockedReason, textAlign: TextAlign.center, style: const TextStyle(color: AppTheme.textMuted)),
        ],
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    Widget body;
    if (error != null) {
      body = ErrorView(message: error!, onRetry: loadProfile);
    } else if (detail == null) {
      body = const LoadingView();
    } else {
      body = RefreshIndicator(onRefresh: loadProfile, child: buildContent(detail!));
    }
    return MasterScreen(title: "Profil majstora", child: body);
  }
}
