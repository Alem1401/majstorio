import 'package:flutter/material.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';

// Small building blocks used by many screens.

// A labeled status, for example "Na čekanju" on an offer.
class StatusChip extends StatelessWidget {
  const StatusChip(this.label, {super.key, this.highlighted = false});

  final String label;
  // Gold background for states that need the user's attention.
  final bool highlighted;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: highlighted ? AppTheme.goldSoft : const Color(0xFFF1EEE5),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: highlighted ? AppTheme.gold : AppTheme.border),
      ),
      child: Text(
        label,
        style: const TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w600,
          color: AppTheme.ink,
        ),
      ),
    );
  }
}

// One line of a detail card: icon, label and value, for example the city of a request.
class InfoRow extends StatelessWidget {
  const InfoRow({super.key, required this.icon, required this.label, required this.value});

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: AppTheme.textMuted),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                const SizedBox(height: 2),
                Text(value, style: const TextStyle(fontSize: 15)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// The heading of a section inside a screen: "Ponude", "Historija statusa"...
class SectionTitle extends StatelessWidget {
  const SectionTitle(this.text, {super.key});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 24, bottom: 8),
      child: Text(
        text,
        style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w700, color: AppTheme.ink),
      ),
    );
  }
}

// The header at the top of a form card: icon in a gold box, title and a short explanation.
// The same header the desktop app uses on its details screens.
class FormHeader extends StatelessWidget {
  const FormHeader({super.key, required this.icon, required this.title, required this.subtitle});

  final IconData icon;
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: AppTheme.goldSoft,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Icon(icon, color: AppTheme.ink),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700, color: AppTheme.ink),
              ),
              const SizedBox(height: 2),
              Text(subtitle, style: const TextStyle(color: AppTheme.textMuted)),
            ],
          ),
        ),
      ],
    );
  }
}

// A picture from the API. Shows an icon when there is no URL or the picture cannot be loaded.
class NetworkPicture extends StatelessWidget {
  const NetworkPicture({
    super.key,
    required this.url,
    this.size = 56,
    this.icon = Icons.person_outline,
    this.width,
    this.height,
  });

  final String? url;
  final double size;
  final IconData icon;
  // Set both for a picture that is not square (a news image).
  final double? width;
  final double? height;

  @override
  Widget build(BuildContext context) {
    var w = width ?? size;
    var h = height ?? size;
    Widget fallback = Container(
      width: w,
      height: h,
      color: AppTheme.goldSoft,
      child: Icon(icon, color: AppTheme.ink),
    );
    var link = url;
    return ClipRRect(
      borderRadius: BorderRadius.circular(12),
      child: link == null || link.isEmpty
          ? fallback
          : Image.network(
              link,
              width: w,
              height: h,
              fit: BoxFit.cover,
              errorBuilder: (context, error, stackTrace) => fallback,
            ),
    );
  }
}

// A small icon with a short text next to it, for the details on list cards (city, date, price).
class IconText extends StatelessWidget {
  const IconText(this.icon, this.text, {super.key});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 16, color: AppTheme.textMuted),
        const SizedBox(width: 4),
        Flexible(child: Text(text, style: const TextStyle(fontSize: 13, color: AppTheme.textMuted))),
      ],
    );
  }
}
