import 'package:flutter/material.dart';
import 'package:majstorio_mobile/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_mobile/screens/tradesperson_profile/tradesperson_profile_details_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';

// One tradesperson with the reason for the recommendation; used by the search and the home screen.
class TradespersonCard extends StatelessWidget {
  const TradespersonCard({super.key, required this.profile});

  final TradespersonProfile profile;

  @override
  Widget build(BuildContext context) {
    var rating = profile.averageRating == null
        ? "Bez ocjena"
        : "${profile.averageRating!.toStringAsFixed(1)} (${profile.reviewCount})";
    var price = profile.minimumPrice == null
        ? null
        : "${formatMoney(profile.minimumPrice!)} – ${formatMoney(profile.maximumPrice ?? profile.minimumPrice!)}";

    return Card(
      child: InkWell(
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (context) => TradespersonProfileDetailsScreen(profileId: profile.id)),
          );
        },
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              NetworkPicture(url: profile.profileImageUrl, size: 64),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      profile.fullName,
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: AppTheme.ink),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      profile.categories.join(", "),
                      style: const TextStyle(color: AppTheme.textMuted),
                    ),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 12,
                      runSpacing: 4,
                      children: [
                        _Metric(icon: Icons.place_outlined, text: profile.cityName),
                        _Metric(icon: Icons.star_outline, text: rating),
                        _Metric(icon: Icons.task_alt, text: "${profile.completedJobs} završenih"),
                        if (price != null) _Metric(icon: Icons.euro, text: price),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Icon(Icons.thumb_up_alt_outlined, size: 16, color: AppTheme.ink),
                        const SizedBox(width: 4),
                        Expanded(
                          child: Text(
                            profile.recommendationText,
                            style: const TextStyle(fontSize: 13, color: AppTheme.ink),
                          ),
                        ),
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
}

class _Metric extends StatelessWidget {
  const _Metric({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 16, color: AppTheme.textMuted),
        const SizedBox(width: 4),
        Text(text, style: const TextStyle(fontSize: 13)),
      ],
    );
  }
}
