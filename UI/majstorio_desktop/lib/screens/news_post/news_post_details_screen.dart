import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';

import 'package:majstorio_desktop/models/news_post/news_post.dart';
import 'package:majstorio_desktop/providers/news_post/news_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:provider/provider.dart';

class NewsPostDetailsScreen extends StatefulWidget {
  const NewsPostDetailsScreen({super.key, this.post});

  final NewsPost? post;

  @override
  State<NewsPostDetailsScreen> createState() => _NewsPostDetailsScreenState();
}

class _NewsPostDetailsScreenState extends State<NewsPostDetailsScreen> {
  late NewsProvider _provider;
  final _formKey = GlobalKey<FormState>();
  final _headlineController = TextEditingController();
  final _contentController = TextEditingController();
  final _imageController = TextEditingController();

  void loadPost(NewsPost newsPost) {
    _headlineController.text = newsPost.title;
    _contentController.text = newsPost.content;
    _imageController.text = newsPost.imageUrl;
  }

  @override
  void initState() {
    super.initState();
    _provider = context.read<NewsProvider>();
    final post = widget.post;
    if (post != null) {
      loadPost(post);
    }
  }

  @override
  void dispose() {
    _headlineController.dispose();
    _contentController.dispose();
    _imageController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;

    var newsPost = NewsPost(
      id: widget.post?.id,
      authorId: 1, // hardcoded for now
      title: _headlineController.text,
      content: _contentController.text,
      imageUrl: _imageController.text,
      publishedAtUtc: DateTime.now().toUtc(),
    );
    final int response;
    if (widget.post != null) {
      response = await _provider.update(newsPost);
    } else {
      response = await _provider.post(newsPost);
    }
    if (!mounted) return;
    if (response == 200) {
      Navigator.pop(context, true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: widget.post == null ? "Add new post" : "Update this post",
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 480),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(32),
                child: Form(
                  key: _formKey,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: AppTheme.goldSoft,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: const Icon(
                              Icons.article_outlined,
                              color: AppTheme.ink,
                            ),
                          ),
                          const SizedBox(width: 16),
                          const Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  "Post details",
                                  style: TextStyle(
                                    fontSize: 20,
                                    fontWeight: FontWeight.w700,
                                    color: AppTheme.ink,
                                  ),
                                ),
                                SizedBox(height: 2),
                                Text(
                                  "Enter the headline, content and image of the post.",
                                  style: TextStyle(color: AppTheme.textMuted),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 24),
                      const Divider(),
                      const SizedBox(height: 24),
                      TextFormField(
                        controller: _headlineController,
                        decoration: const InputDecoration(
                          labelText: "Post headline",
                          prefixIcon: Icon(Icons.title),
                        ),
                        validator: (value) {
                          if (value == null || value.trim().isEmpty) {
                            return "Headline is required";
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _contentController,
                        minLines: 3,
                        maxLines: 6,
                        decoration: const InputDecoration(
                          labelText: "Content",
                          prefixIcon: Icon(Icons.notes_outlined),
                        ),
                        validator: (value) {
                          if (value == null || value.trim().isEmpty) {
                            return "There needs to be some content";
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _imageController,
                        decoration: const InputDecoration(
                          labelText: "Image url",
                          prefixIcon: Icon(Icons.image_outlined),
                        ),
                        validator: (value) {
                          if (value == null || value.trim().isEmpty) {
                            return "You must have image url";
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 24),
                      Align(
                        alignment: Alignment.centerRight,
                        child: ElevatedButton.icon(
                          onPressed: _save,
                          icon: const Icon(Icons.add),
                          label: Text(widget.post == null ? "Add" : "Update"),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
