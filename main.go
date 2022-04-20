package main

import (
	"log"
	"time"

	"github.com/gofiber/fiber/v2"
	"github.com/google/go-github/v43/github"
	"github.com/jellydator/ttlcache/v3"
)

func main() {
	app := fiber.New()
	hub := github.NewClient(nil)
	cache := ttlcache.New(
		ttlcache.WithTTL[string, fiber.Map](30 * time.Minute),
	)

	go cache.Start()

	app.Get("/releases/latest", func(c *fiber.Ctx) error {
		cached := cache.Get("cached-release")

		if cached == nil {
			log.Println("Release not in cache")

			release, _, err := hub.Repositories.GetLatestRelease(
				c.UserContext(),
				"picotorrent",
				"picotorrent")

			if err != nil {
				return c.Status(500).SendString(err.Error())
			}

			markdown, _, err := hub.Markdown(
				c.UserContext(),
				*release.Body,
				&github.MarkdownOptions{
					Mode:    "gfm",
					Context: "picotorrent/picotorrent",
				})

			if err != nil {
				return c.Status(500).SendString(err.Error())
			}

			// map assets
			var assets []fiber.Map

			for i := 0; i < len(release.Assets); i++ {
				assets = append(assets, fiber.Map{
					"name": release.Assets[i].Name,
					"url":  release.Assets[i].BrowserDownloadURL,
				})
			}

			cached = cache.Set(
				"cached-release",
				fiber.Map{
					"assets":  assets,
					"notes":   markdown,
					"url":     *release.HTMLURL,
					"version": *release.TagName,
				},
				30*time.Minute)
		}

		return c.JSON(cached.Value())
	})

	log.Fatal(app.Listen(":3000"))
}
